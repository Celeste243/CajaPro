using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using CajaPro.Logica;
using CajaPro.Modelos;
namespace CajaPro
{
    public partial class FormClientes : Form
    {
        public FormClientes()
        {
            InitializeComponent();
        }

        private void FormClientes_Load(object sender, EventArgs e)
        {
            RefrescarTabla();
        }

        private void RefrescarTabla()
        {
            dgvClientes.Rows.Clear();
            foreach (var cli in SupermercadoEngine.Clientes)
            {
                string estado = cli.Activo ? "Activo" : "Suspendido (Baja Lógica)";
                dgvClientes.Rows.Add(cli.Id, cli.Nombre, cli.Dni, estado);
            }
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            try
            {
                string id = txtIDCliente.Text.Trim();
                string nombre = txtNombre.Text.Trim();
                string dni = txtDNI.Text.Trim();

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(dni))
                {
                    MessageBox.Show("Todos los campos son obligatorios para el registro.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (id.ToUpper() == "C0")
                {
                    MessageBox.Show("El ID 'C0' está reservado para Consumidor Final.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                var existente = SupermercadoEngine.Clientes.FirstOrDefault(c => c.Id == id);

                if (existente != null)
                {
                    existente.Nombre = nombre;
                    existente.Dni = dni;
                    existente.Activo = true;
                    MessageBox.Show("¡Datos del cliente actualizados!", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    SupermercadoEngine.Clientes.Add(new Cliente
                    {
                        Id = id,
                        Nombre = nombre,
                        Dni = dni,
                        Activo = true
                    });
                    MessageBox.Show("¡Nuevo cliente registrado con éxito!", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                RefrescarTabla();
                LimpiarCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBaja_Click(object sender, EventArgs e)
        {
            try
            {
                string id = txtIDCliente.Text.Trim();

                if (id.ToUpper() == "C0")
                {
                    MessageBox.Show("No se puede dar de baja al Consumidor Final.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                var cliente = SupermercadoEngine.Clientes.FirstOrDefault(c => c.Id == id);

                if (cliente == null)
                {
                    MessageBox.Show("Ingrese un ID de cliente válido.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool tieneVentas = SupermercadoEngine.HistorialVentas.Any(v => v.IdCliente == id);

                if (tieneVentas)
                {
                    cliente.Activo = false;
                    MessageBox.Show("El cliente registra compras. Se aplicó una BAJA LÓGICA.", "CajaPro - Integridad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    SupermercadoEngine.Clientes.Remove(cliente);
                    MessageBox.Show("Cliente eliminado físicamente.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                RefrescarTabla();
                LimpiarCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar baja: {ex.Message}", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LimpiarCampos()
        {
            txtIDCliente.Clear();
            txtNombre.Clear();
            txtDNI.Clear();
            txtIDCliente.Focus();
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
        // PUENTES PARA EL DISEÑADOR (Evitan los errores CS1061)
        // ========================================================
        private void btnGuardar_Click_1(object sender, EventArgs e)
        {
            btnGuardar_Click(sender, e);
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