using MySql.Data.MySqlClient;
using CajaPro.Datos;
using CajaPro.Logica;
using CajaPro.Modelos;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace CajaPro
{
    public partial class FormProductos : Form
    {
        public FormProductos()
        {
            InitializeComponent();
        }

        private void FormProductos_Load(object sender, EventArgs e)
        {
            RefrescarTabla();
        }

        private void RefrescarTabla()
        {
            dgvProductos.Rows.Clear();
            Conexion cn = new Conexion();
            try
            {
                MySqlConnection conexion = cn.AbrirConexion();
                MySqlCommand cmd = new MySqlCommand(
                    "SELECT Codigo, Nombre, Precio, Stock, Estado FROM Productos ORDER BY Nombre",
                    conexion);

                using (MySqlDataReader lector = cmd.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        string estado = (bool)lector["Estado"] ? "Disponible" : "Baja Lógica";
                        dgvProductos.Rows.Add(
                            lector["Codigo"],
                            lector["Nombre"],
                            ((decimal)lector["Precio"]).ToString("F2"),
                            lector["Stock"],
                            estado);
                    }
                }
            }
            finally
            {
                cn.CerrarConexion();
            }
        }

        // ==========================================
        // REQUERIMIENTO: ALTA / MODIFICACIÓN DE PRODUCTO
        // ==========================================
        private void btnGuardar_Click(object sender, EventArgs e)
        {
            string codigo = txtCodigo.Text.Trim();
            string nombre = txtNombre.Text.Trim();

            if (codigo == "" || nombre == "")
            {
                MessageBox.Show("El código y el nombre son obligatorios.");
                return;
            }
            if (!decimal.TryParse(txtPrecio.Text, out decimal precio) || precio <= 0)
            {
                MessageBox.Show("Ingrese un precio válido mayor a 0.");
                return;
            }

            Conexion cn = new Conexion();
            try
            {
                MySqlConnection conexion = cn.AbrirConexion();

                // ¿Ya existe un producto con ese código?
                MySqlCommand cmdExiste = new MySqlCommand(
                    "SELECT COUNT(*) FROM Productos WHERE Codigo = @Codigo", conexion);
                cmdExiste.Parameters.AddWithValue("@Codigo", codigo);
                bool existe = (int)cmdExiste.ExecuteScalar() > 0;

                string sql = existe
                    ? "UPDATE Productos SET Nombre=@Nombre, Categoria=@Categoria, Precio=@Precio, Stock=@Stock, Estado=1 WHERE Codigo=@Codigo"
                    : "INSERT INTO Productos (Codigo, Nombre, Categoria, Precio, Stock, Estado) VALUES (@Codigo, @Nombre, @Categoria, @Precio, @Stock, 1)";

                MySqlCommand cmd = new MySqlCommand(sql, conexion);
                cmd.Parameters.AddWithValue("@Codigo", codigo);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@Categoria", textBox1.Text.Trim());
                cmd.Parameters.AddWithValue("@Precio", precio);
                cmd.Parameters.AddWithValue("@Stock", (int)numericUpDown1.Value);
                cmd.ExecuteNonQuery();

                MessageBox.Show(existe ? "¡Producto modificado!" : "¡Producto agregado!");
                LimpiarCampos();
                RefrescarTabla();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                cn.CerrarConexion();
            }
        }
        private void btnBaja_Click(object sender, EventArgs e)
        {
            string codigo = txtCodigo.Text.Trim();
            if (codigo == "")
            {
                MessageBox.Show("Seleccione o escriba un código de producto.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Conexion cn = new Conexion();
            try
            {
                MySqlConnection conexion = cn.AbrirConexion();

                // 1) Buscar el producto
                MySqlCommand cmdId = new MySqlCommand(
                    "SELECT IdProductos FROM Productos WHERE Codigo = @Codigo", conexion);
                cmdId.Parameters.AddWithValue("@Codigo", codigo);
                object resultado = cmdId.ExecuteScalar();

                if (resultado == null)
                {
                    MessageBox.Show("No existe un producto con ese código.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                int idProducto = (int)resultado;

                // 2) ¿Ya se vendió alguna vez?
                MySqlCommand cmdVentas = new MySqlCommand(
                    "SELECT COUNT(*) FROM DetalleVenta WHERE IdProductos = @Id", conexion);
                cmdVentas.Parameters.AddWithValue("@Id", idProducto);
                bool yaSeVendio = (int)cmdVentas.ExecuteScalar() > 0;

                if (yaSeVendio)
                {
                    MySqlCommand cmdBaja = new MySqlCommand(
                        "UPDATE Productos SET Estado = 0 WHERE IdProductos = @Id", conexion);
                    cmdBaja.Parameters.AddWithValue("@Id", idProducto);
                    cmdBaja.ExecuteNonQuery();
                    MessageBox.Show("El producto tiene historial de ventas. Se aplicó una BAJA LÓGICA.", "CajaPro - Integridad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MySqlCommand cmdBorrar = new MySqlCommand(
                        "DELETE FROM Productos WHERE IdProductos = @Id", conexion);
                    cmdBorrar.Parameters.AddWithValue("@Id", idProducto);
                    cmdBorrar.ExecuteNonQuery();
                    MessageBox.Show("Producto eliminado por no registrar ventas anteriores.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                LimpiarCampos();
                RefrescarTabla();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al procesar la baja: " + ex.Message, "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cn.CerrarConexion();
            }
        }


        private void ModificarProducto()
        {
            string codigo = txtCodigo.Text.Trim();
            string nombre = txtNombre.Text.Trim();

            if (codigo == "")
            {
                MessageBox.Show("Tocá primero un producto de la tabla.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (nombre == "")
            {
                MessageBox.Show("El nombre es obligatorio.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!decimal.TryParse(txtPrecio.Text, out decimal precio) || precio <= 0)
            {
                MessageBox.Show("Ingrese un precio válido mayor a 0.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Conexion cn = new Conexion();
            try
            {
                MySqlCommand cmd = new MySqlCommand(
                    "UPDATE Productos SET Nombre=@Nombre, Categoria=@Categoria, Precio=@Precio, Stock=@Stock " +
                    "WHERE Codigo=@Codigo",
                    cn.AbrirConexion());
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@Categoria", textBox1.Text.Trim());
                cmd.Parameters.AddWithValue("@Precio", precio);
                cmd.Parameters.AddWithValue("@Stock", (int)numericUpDown1.Value);
                cmd.Parameters.AddWithValue("@Codigo", codigo);

                if (cmd.ExecuteNonQuery() == 0)
                {
                    MessageBox.Show("No existe un producto con ese código. Tocá una fila de la tabla.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                MessageBox.Show("¡Producto modificado!", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LimpiarCampos();
                RefrescarTabla();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al modificar: " + ex.Message, "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cn.CerrarConexion();
            }
        }

        private void EliminarProducto()
        {
            string codigo = txtCodigo.Text.Trim();
            if (codigo == "")
            {
                MessageBox.Show("Tocá primero un producto de la tabla.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult respuesta = MessageBox.Show(
                "¿Seguro que querés eliminar este producto? No se puede deshacer.",
                "CajaPro", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (respuesta != DialogResult.Yes) return;

            Conexion cn = new Conexion();
            try
            {
                MySqlConnection con = cn.AbrirConexion();

                // ¿Ya se vendió alguna vez?
                MySqlCommand cmdVentas = new MySqlCommand(
                    "SELECT COUNT(*) FROM DetalleVenta d " +
                    "JOIN Productos p ON p.IdProductos = d.IdProductos " +
                    "WHERE p.Codigo = @Codigo", con);
                cmdVentas.Parameters.AddWithValue("@Codigo", codigo);

                if ((int)cmdVentas.ExecuteScalar() > 0)
                {
                    MessageBox.Show("Este producto ya tiene ventas, no se puede eliminar. Usá \"Dar de baja\".",
                        "CajaPro - Integridad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                MySqlCommand cmd = new MySqlCommand("DELETE FROM Productos WHERE Codigo = @Codigo", con);
                cmd.Parameters.AddWithValue("@Codigo", codigo);

                if (cmd.ExecuteNonQuery() == 0)
                {
                    MessageBox.Show("No existe un producto con ese código.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                MessageBox.Show("Producto eliminado.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LimpiarCampos();
                RefrescarTabla();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al eliminar: " + ex.Message, "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cn.CerrarConexion();
            }
        }

        private void LimpiarCampos()
        {
            txtCodigo.Clear();
            txtNombre.Clear();
            txtPrecio.Clear();
            textBox1.Clear();
            numericUpDown1.Value = 0;
            txtCodigo.Focus();
        }
        private void dgvProductos_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvProductos.Rows[e.RowIndex];
            txtCodigo.Text = row.Cells[0].Value?.ToString();
            txtNombre.Text = row.Cells[1].Value?.ToString();
            txtPrecio.Text = row.Cells[2].Value?.ToString();
            if (int.TryParse(row.Cells[3].Value?.ToString(), out int stockVal))
                numericUpDown1.Value = stockVal;

            // La categoría no está en la grilla: la buscamos en la base
            Conexion cn = new Conexion();
            try
            {
                MySqlCommand cmd = new MySqlCommand(
                    "SELECT Categoria FROM Productos WHERE Codigo = @Codigo", cn.AbrirConexion());
                cmd.Parameters.AddWithValue("@Codigo", txtCodigo.Text);
                textBox1.Text = cmd.ExecuteScalar()?.ToString() ?? "";
            }
            finally
            {
                cn.CerrarConexion();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ModificarProducto();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            EliminarProducto();
        }
    }
}

        