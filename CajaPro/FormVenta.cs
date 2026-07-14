using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using CajaPro.Logica;
using CajaPro.Modelos;

namespace CajaPro
{
    public partial class FormVenta : Form
    {
        // Carrito de compras temporal para la sesión actual de la caja
        private List<DetalleVenta> carritoActual = new List<DetalleVenta>();
        public static bool ModoIncognitoActivo = false;
        public FormVenta()
        {
            InitializeComponent();
        }

        // ==========================================
        // CARGA INICIAL: Llena el combo con los clientes
        // ==========================================
        private void FormVenta_Load(object sender, EventArgs e)
        {
            CargarClientes();
            LimpiarPantallaVenta();
        }

        private void CargarClientes()
        {
            cbClientes.DataSource = null;
            // Trae los clientes activos del motor de CajaPro
            cbClientes.DataSource = SupermercadoEngine.Clientes.Where(c => c.Activo).ToList();
            cbClientes.DisplayMember = "Nombre";
            cbClientes.ValueMember = "Id";
        }

        // ==========================================
        // BOTÓN: AGREGAR PRODUCTO AL CARRITO
        // ==========================================
        private void btnAgregar_Click(object sender, EventArgs e)
        {
            try
            {
                string codigo = txtCodigoProd.Text.Trim();

                if (string.IsNullOrEmpty(codigo))
                {
                    MessageBox.Show("Por favor, ingrese o escanee el código del producto.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(txtCantidad.Text, out int cantidad) || cantidad <= 0)
                {
                    MessageBox.Show("Ingrese una cantidad válida mayor a cero.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Buscamos el producto en las góndolas virtuales
                var prod = SupermercadoEngine.Productos.FirstOrDefault(p => p.Id == codigo && p.Activo);

                if (prod == null)
                {
                    MessageBox.Show("El código ingresado no corresponde a un producto activo.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Verificamos si hay stock suficiente antes de sumarlo visualmente
                if (prod.Stock < cantidad)
                {
                    MessageBox.Show($"Stock insuficiente. Quedan solo {prod.Stock} unidades en góndola.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Si el producto ya estaba en el carrito, sumamos la cantidad
                var itemYaEnCarrito = carritoActual.FirstOrDefault(i => i.IdProducto == codigo);
                if (itemYaEnCarrito != null)
                {
                    if (prod.Stock < (itemYaEnCarrito.Cantidad + cantidad))
                    {
                        MessageBox.Show("No se puede agregar esa cantidad. Supera el stock disponible.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    itemYaEnCarrito.Cantidad += cantidad;
                }
                else
                {
                    // Si es nuevo, lo agregamos al carrito temporal
                    carritoActual.Add(new DetalleVenta
                    {
                        IdProducto = prod.Id,
                        NombreProducto = prod.Nombre,
                        PrecioUnitario = prod.Precio,
                        Cantidad = cantidad
                    });
                }

                ActualizarGrillaYTotal();
                txtCodigoProd.Clear();
                txtCantidad.Text = "1";
                txtCodigoProd.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar: {ex.Message}", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==========================================
        // BOTÓN: COBRAR Y REGISTRAR LA FACTURA
        // ==========================================
        private void btnCobrar_Click(object sender, EventArgs e)
        {
            try
            {
                if (carritoActual.Count == 0)
                {
                    MessageBox.Show("El carrito está vacío. Cargue productos antes de cobrar.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cbClientes.SelectedValue == null)
                {
                    MessageBox.Show("Seleccione un cliente válido para la facturación.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string idCliente = cbClientes.SelectedValue.ToString();

                // Enviamos el carrito al motor lógico para procesar la transacción y descontar stock
                SupermercadoEngine.RegistrarVenta(idCliente, carritoActual);

                MessageBox.Show("¡Venta procesada con éxito!\nTicket grabado en el historial de CajaPro.", "CajaPro - Facturación", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Limpiamos todo para la siguiente venta
                carritoActual.Clear();
                ActualizarGrillaYTotal();
                LimpiarPantallaVenta();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar el cobro: {ex.Message}", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            txtCodigoProd.Clear();
            txtCantidad.Text = "1";
            if (cbClientes.Items.Count > 0) cbClientes.SelectedIndex = 0;
            lblTotal.Text = "TOTAL: $0.00";
        }

        private void FormVenta_Load_1(object sender, EventArgs e)
        {
            FormVenta_Load(sender, e);
        }

        private void txtCodigoProducto_TextChanged(object sender, EventArgs e)
        {

        }
    }
}