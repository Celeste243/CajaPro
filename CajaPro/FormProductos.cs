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
            foreach (var prod in SupermercadoEngine.Productos)
            {
                string estado = prod.Activo ? "Disponible" : "Baja Lógica";
                dgvProductos.Rows.Add(prod.Id, prod.Nombre, prod.Precio.ToString("F2"), prod.Stock, estado);
            }
        }

        // ==========================================
        // REQUERIMIENTO: ALTA / MODIFICACIÓN DE PRODUCTO
        // ==========================================
        private void btnGuardar_Click(object sender, EventArgs e)
        {
            try
            {
                string codigo = txtCodigo.Text.Trim();
                string nombre = txtNombre.Text.Trim();

                if (string.IsNullOrEmpty(codigo) || string.IsNullOrEmpty(nombre))
                {
                    MessageBox.Show("El código y el nombre son obligatorios.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtPrecio.Text, out decimal precio) || precio <= 0)
                {
                    MessageBox.Show("Ingrese un precio válido mayor a 0.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(txtStock.Text, out int stock) || stock < 0)
                {
                    MessageBox.Show("Ingrese un stock válido (0 o más).", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Buscamos si el producto ya existe para actualizarlo o crearlo nuevo
                var existente = SupermercadoEngine.Productos.FirstOrDefault(p => p.Id == codigo);

                if (existente != null)
                {
                    // Modificación
                    existente.Nombre = nombre;
                    existente.Precio = precio;
                    existente.Stock = stock;
                    existente.Activo = true; // Si estaba de baja, lo reactiva al actualizar stock
                    MessageBox.Show("¡Producto modificado en el inventario!", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Alta Nueva
                    SupermercadoEngine.Productos.Add(new Producto
                    {
                        Id = codigo,
                        Nombre = nombre,
                        Precio = precio,
                        Stock = stock,
                        Activo = true
                    });
                    MessageBox.Show("¡Nuevo producto agregado a las góndolas!", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                RefrescarTabla();
                LimpiarCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==========================================
        // REQUERIMIENTO: BAJA LÓGICA (CON DEPENDENCIAS)
        // ==========================================
        private void btnBaja_Click(object sender, EventArgs e)
        {
            try
            {
                string codigo = txtCodigo.Text.Trim();
                var prod = SupermercadoEngine.Productos.FirstOrDefault(p => p.Id == codigo);

                if (prod == null)
                {
                    MessageBox.Show("Seleccione o escriba un código de producto válido.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // VALIDACIÓN CLAVE DE EXAMEN: Verificar si ya se vendió antes de borrar
                bool yaSeVendio = SupermercadoEngine.HistorialVentas.Any(v => v.Items.Any(i => i.IdProducto == codigo));

                if (yaSeVendio)
                {
                    // Bloqueo lógico para no romper los tickets viejos de la caja
                    prod.Activo = false;
                    MessageBox.Show("El producto tiene historial de ventas. Se aplicó una BAJA LÓGICA (Deshabilitado de góndolas para no romper registros).", "CajaPro - Integridad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    // Borrado físico directo porque nunca se usó
                    SupermercadoEngine.Productos.Remove(prod);
                    MessageBox.Show("Producto eliminado físicamente del sistema por no registrar ventas anteriores.", "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            txtCodigo.Clear();
            txtNombre.Clear();
            txtPrecio.Clear();
            txtStock.Clear();
            txtCodigo.Focus();
        }

        // Evento extra: Si el usuario hace clic en una fila de la tabla, carga los datos en los campos de arriba
        private void dgvProductos_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvProductos.Rows[e.RowIndex];
                txtCodigo.Text = row.Cells[0].Value?.ToString();
                txtNombre.Text = row.Cells[1].Value?.ToString();
                txtPrecio.Text = row.Cells[2].Value?.ToString();
                txtStock.Text = row.Cells[3].Value?.ToString();
            }
        }
    }
}