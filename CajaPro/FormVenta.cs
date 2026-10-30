using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Windows.Forms;
using CajaPro.Datos;
using CajaPro.Modelos;

namespace CajaPro
{
    public partial class FormVenta : Form
    {
        // Carrito temporal de la venta en curso (IdProducto guarda el CÓDIGO de barras)
        private List<DetalleVenta> carritoActual = new List<DetalleVenta>();
        public static bool ModoIncognitoActivo = false;

        public FormVenta()
        {
            InitializeComponent();
        }

        // ==========================================
        // CARGA INICIAL
        // ==========================================
        private void FormVenta_Load(object sender, EventArgs e)
        {
            CargarClientes();
            LimpiarPantallaVenta();
        }

        private void CargarClientes()
        {
            DataTable tabla = new DataTable();
            Conexion cn = new Conexion();
            try
            {
                // Consumidor Final primero, después el resto por nombre
                MySqlCommand cmd = new MySqlCommand(
                    "SELECT IdCliente, RTRIM(Nombre) AS Nombre FROM Clientes WHERE Estado = 1 " +
                    "ORDER BY CASE WHEN DNI = '00000000' THEN 0 ELSE 1 END, Nombre",
                    cn.AbrirConexion());

                using (MySqlDataReader lector = cmd.ExecuteReader())
                {
                    tabla.Load(lector);
                }
            }
            finally
            {
                cn.CerrarConexion();
            }

            cmbCliente.DisplayMember = "Nombre";
            cmbCliente.ValueMember = "IdCliente";
            cmbCliente.DataSource = tabla;
        }

        // ==========================================
        // BOTÓN: AGREGAR PRODUCTO AL CARRITO
        // ==========================================
        private void btnAgregar_Click(object sender, EventArgs e)
        {
            try
            {
                string codigo = txtCodigoProducto.Text.Trim();

                if (string.IsNullOrEmpty(codigo))
                {
                    MessageBox.Show("Por favor, ingrese o escanee el código del producto.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int cantidad = (int)numericUpDown1.Value;
                if (cantidad <= 0)
                {
                    MessageBox.Show("Ingrese una cantidad válida mayor a cero.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Buscar el producto en la base
                bool encontrado = false;
                string nombre = "";
                decimal precio = 0;
                int stock = 0;

                Conexion cn = new Conexion();
                try
                {
                    MySqlCommand cmd = new MySqlCommand(
                        "SELECT RTRIM(Nombre) AS Nombre, Precio, Stock FROM Productos WHERE Codigo = @Codigo AND Estado = 1",
                        cn.AbrirConexion());
                    cmd.Parameters.AddWithValue("@Codigo", codigo);

                    using (MySqlDataReader lector = cmd.ExecuteReader())
                    {
                        if (lector.Read())
                        {
                            encontrado = true;
                            nombre = lector["Nombre"].ToString();
                            precio = (decimal)lector["Precio"];
                            stock = (int)lector["Stock"];
                        }
                    }
                }
                finally
                {
                    cn.CerrarConexion();
                }

                if (!encontrado)
                {
                    MessageBox.Show("El código ingresado no corresponde a un producto activo.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // ¿Cuántas unidades de este producto ya hay en el carrito?
                var itemYaEnCarrito = carritoActual.FirstOrDefault(i => i.IdProducto == codigo);
                int yaEnCarrito = itemYaEnCarrito != null ? itemYaEnCarrito.Cantidad : 0;

                if (stock < yaEnCarrito + cantidad)
                {
                    MessageBox.Show($"Stock insuficiente. Quedan {stock} unidades y ya tenés {yaEnCarrito} en el carrito.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (itemYaEnCarrito != null)
                {
                    itemYaEnCarrito.Cantidad += cantidad;
                }
                else
                {
                    carritoActual.Add(new DetalleVenta
                    {
                        IdProducto = codigo,
                        NombreProducto = nombre,
                        PrecioUnitario = precio,
                        Cantidad = cantidad
                    });
                }

                ActualizarGrillaYTotal();
                txtCodigoProducto.Clear();
                numericUpDown1.Value = 1;
                txtCodigoProducto.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar: {ex.Message}", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==========================================
        // BOTÓN: COBRAR Y REGISTRAR LA VENTA
        // ==========================================
        private void btnCobrar_Click(object sender, EventArgs e)
        {
            if (carritoActual.Count == 0)
            {
                MessageBox.Show("El carrito está vacío. Cargue productos antes de cobrar.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbCliente.SelectedValue == null)
            {
                MessageBox.Show("Seleccione un cliente válido para la facturación.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int idCliente = Convert.ToInt32(cmbCliente.SelectedValue);
            decimal total = carritoActual.Sum(i => i.Subtotal);

            Conexion cn = new Conexion();
            MySqlTransaction tran = null;
            try
            {
                MySqlConnection con = cn.AbrirConexion();
                tran = con.BeginTransaction();

                // 1) Descontar el stock (solo si todavía alcanza)
                foreach (var item in carritoActual)
                {
                    MySqlCommand cmdStock = new MySqlCommand(
                        "UPDATE Productos SET Stock = Stock - @Cant " +
                        "WHERE Codigo = @Codigo AND Estado = 1 AND Stock >= @Cant",
                        con, tran);
                    cmdStock.Parameters.AddWithValue("@Cant", item.Cantidad);
                    cmdStock.Parameters.AddWithValue("@Codigo", item.IdProducto);

                    if (cmdStock.ExecuteNonQuery() == 0)
                        throw new Exception($"No hay stock suficiente de {item.NombreProducto}.");
                }

                // 2) Guardar la venta y obtener su número
                MySqlCommand cmdVenta = new MySqlCommand(
                    "INSERT INTO Ventas (Fecha, IdCliente, Total) " +
                    "OUTPUT INSERTED.IdVenta VALUES (GETDATE(), @IdCliente, @Total)",
                    con, tran);
                cmdVenta.Parameters.AddWithValue("@IdCliente", idCliente);
                cmdVenta.Parameters.AddWithValue("@Total", total);
                int idVenta = (int)cmdVenta.ExecuteScalar();

                // 3) Guardar cada renglón del ticket
                foreach (var item in carritoActual)
                {
                    MySqlCommand cmdDetalle = new MySqlCommand(
                        "INSERT INTO DetalleVenta (IdVenta, IdProductos, Cantidad, PrecioUnitario) " +
                        "SELECT @IdVenta, IdProductos, @Cant, @Precio FROM Productos WHERE Codigo = @Codigo",
                        con, tran);
                    cmdDetalle.Parameters.AddWithValue("@IdVenta", idVenta);
                    cmdDetalle.Parameters.AddWithValue("@Cant", item.Cantidad);
                    cmdDetalle.Parameters.AddWithValue("@Precio", item.PrecioUnitario);
                    cmdDetalle.Parameters.AddWithValue("@Codigo", item.IdProducto);
                    cmdDetalle.ExecuteNonQuery();
                }

                // Todo salió bien: confirmar
                tran.Commit();

                MessageBox.Show($"¡Venta procesada con éxito!\nTicket N° {idVenta}  -  Total: ${total:F2}",
                    "CajaPro - Facturación", MessageBoxButtons.OK, MessageBoxIcon.Information);

                carritoActual.Clear();
                ActualizarGrillaYTotal();
                LimpiarPantallaVenta();
            }
            catch (Exception ex)
            {
                // Algo falló: deshacer todo para que no quede nada a medias
                if (tran != null) tran.Rollback();
                MessageBox.Show($"Error al procesar el cobro: {ex.Message}", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cn.CerrarConexion();
            }
        }

        public void ActualizarGrillaYTotal()
        {
            dgvCarrito.Rows.Clear();
            decimal totalAcumulado = 0;

            foreach (var item in carritoActual)
            {
                dgvCarrito.Rows.Add(item.IdProducto, item.NombreProducto, item.PrecioUnitario.ToString("F2"), item.Cantidad, item.Subtotal.ToString("F2"));
                totalAcumulado += item.Subtotal;
            }

            if (ModoIncognitoActivo)
            {
                lblTotal.Text = "TOTAL: $ ***";
            }
            else
            {
                lblTotal.Text = $"TOTAL: ${totalAcumulado.ToString("F2")}";
            }
        }

        private void LimpiarPantallaVenta()
        {
            txtCodigoProducto.Clear();
            numericUpDown1.Value = 1;
            if (cmbCliente.Items.Count > 0) cmbCliente.SelectedIndex = 0;
            lblTotal.Text = "TOTAL: $0.00";
        }

        // ========================================================
        // MÉTODOS QUE USA EL DISEÑADOR (se mantienen aunque estén vacíos)
        // ========================================================
        private void FormVenta_Load_1(object sender, EventArgs e)
        {
            FormVenta_Load(sender, e);
        }

        private void txtCodigoProducto_TextChanged(object sender, EventArgs e)
        {

        }

        private void lblFormaPago_Click(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }
    }
}