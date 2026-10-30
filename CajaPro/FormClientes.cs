using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Windows.Forms;
using CajaPro.Datos;

namespace CajaPro
{
    public partial class FormClientes : Form
    {
        // DNI reservado para el cliente "Consumidor Final"
        private const string DniConsumidorFinal = "00000000";

        public FormClientes()
        {
            InitializeComponent();
        }

        private void FormClientes_Load(object sender, EventArgs e)
        {
            txtIDCliente.ReadOnly = true; // el ID lo numera la base
            RefrescarTabla();
        }

        private void RefrescarTabla()
        {
            dgvClientes.Rows.Clear();
            Conexion cn = new Conexion();
            try
            {
                MySqlCommand cmd = new MySqlCommand(
                    "SELECT IdCliente, Nombre, DNI, Estado FROM Clientes ORDER BY Nombre",
                    cn.AbrirConexion());

                using (MySqlDataReader lector = cmd.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        string estado = (bool)lector["Estado"] ? "Activo" : "Suspendido (Baja Lógica)";
                        dgvClientes.Rows.Add(
                            lector["IdCliente"],
                            lector["Nombre"].ToString().Trim(),
                            lector["DNI"],
                            estado);
                    }
                }
            }
            finally
            {
                cn.CerrarConexion();
            }
        }

        // ¿Este cliente es el Consumidor Final?
        private bool EsConsumidorFinal(MySqlConnection con, int idCliente)
        {
            MySqlCommand cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM Clientes WHERE IdCliente = @Id AND DNI = @Dni", con);
            cmd.Parameters.AddWithValue("@Id", idCliente);
            cmd.Parameters.AddWithValue("@Dni", DniConsumidorFinal);
            return (int)cmd.ExecuteScalar() > 0;
        }

        // ==========================================
        // ALTA / MODIFICACIÓN DE CLIENTE
        // ==========================================
        private void btnGuardar_Click(object sender, EventArgs e)
        {
            string nombre = txtNombre.Text.Trim();
            string dni = txtDNI.Text.Trim();

            if (nombre == "" || dni == "")
            {
                MessageBox.Show("El nombre y el DNI son obligatorios.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dni == DniConsumidorFinal)
            {
                MessageBox.Show("El DNI 00000000 está reservado para Consumidor Final.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            // Si el cuadro ID tiene número, estamos modificando; si está vacío, es uno nuevo
            bool esEdicion = int.TryParse(txtIDCliente.Text, out int id);
            if (!esEdicion) id = 0;

            Conexion cn = new Conexion();
            try
            {
                MySqlConnection con = cn.AbrirConexion();

                if (esEdicion && EsConsumidorFinal(con, id))
                {
                    MessageBox.Show("No se puede modificar al Consumidor Final.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                // El DNI no puede estar repetido en otro cliente
                MySqlCommand cmdDni = new MySqlCommand(
                    "SELECT COUNT(*) FROM Clientes WHERE DNI = @Dni AND IdCliente <> @Id", con);
                cmdDni.Parameters.AddWithValue("@Dni", dni);
                cmdDni.Parameters.AddWithValue("@Id", id);
                if ((int)cmdDni.ExecuteScalar() > 0)
                {
                    MessageBox.Show("Ya existe otro cliente con ese DNI.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string sql = esEdicion
                    ? "UPDATE Clientes SET Nombre = @Nombre, DNI = @Dni, Estado = 1 WHERE IdCliente = @Id"
                    : "INSERT INTO Clientes (Nombre, DNI, Estado) VALUES (@Nombre, @Dni, 1)";

                MySqlCommand cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@Dni", dni);
                if (esEdicion) cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();

                MessageBox.Show(esEdicion ? "¡Datos del cliente actualizados!" : "¡Nuevo cliente registrado con éxito!",
                    "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LimpiarCampos();
                RefrescarTabla();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cn.CerrarConexion();
            }
        }

        // ==========================================
        // BAJA (LÓGICA SI TIENE VENTAS, FÍSICA SI NO)
        // ==========================================
        private void btnBaja_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtIDCliente.Text, out int id))
            {
                MessageBox.Show("Tocá primero un cliente de la tabla.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Conexion cn = new Conexion();
            try
            {
                MySqlConnection con = cn.AbrirConexion();

                if (EsConsumidorFinal(con, id))
                {
                    MessageBox.Show("No se puede dar de baja al Consumidor Final.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                MySqlCommand cmdVentas = new MySqlCommand(
                    "SELECT COUNT(*) FROM Ventas WHERE IdCliente = @Id", con);
                cmdVentas.Parameters.AddWithValue("@Id", id);
                bool tieneVentas = (int)cmdVentas.ExecuteScalar() > 0;

                if (tieneVentas)
                {
                    MySqlCommand cmdBaja = new MySqlCommand(
                        "UPDATE Clientes SET Estado = 0 WHERE IdCliente = @Id", con);
                    cmdBaja.Parameters.AddWithValue("@Id", id);
                    cmdBaja.ExecuteNonQuery();
                    MessageBox.Show("El cliente registra compras. Se aplicó una BAJA LÓGICA.", "CajaPro - Integridad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MySqlCommand cmdBorrar = new MySqlCommand(
                        "DELETE FROM Clientes WHERE IdCliente = @Id", con);
                    cmdBorrar.Parameters.AddWithValue("@Id", id);
                    cmdBorrar.ExecuteNonQuery();
                    MessageBox.Show("Cliente eliminado.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                LimpiarCampos();
                RefrescarTabla();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al procesar baja: " + ex.Message, "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cn.CerrarConexion();
            }
        }

        private void LimpiarCampos()
        {
            txtIDCliente.Clear();
            txtNombre.Clear();
            txtDNI.Clear();
            txtNombre.Focus();
        }

        private void dgvClientes_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvClientes.Rows[e.RowIndex];
                txtIDCliente.Text = row.Cells[0].Value?.ToString();
                txtNombre.Text = row.Cells[1].Value?.ToString();
                txtDNI.Text = row.Cells[2].Value?.ToString();
            }
        }

        // ========================================================
        // PUENTES PARA EL DISEÑADOR (los botones del formulario
        // están conectados a estos nombres, así que se mantienen)
        // ========================================================
        private void btnGuardar_Click_1(object sender, EventArgs e)
        {
            btnGuardar_Click(sender, e);
            MessageBox.Show("Entró a Guardar");
        }

        private void btnBaja_Click_1(object sender, EventArgs e)
        {
            btnBaja_Click(sender, e);
        }

        private void dgvClientes_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            dgvClientes_CellClick(sender, e);
        }
    }
}