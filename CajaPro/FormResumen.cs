using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using CajaPro.Datos;

namespace CajaPro
{
    public partial class FormResumen : Form
    {
        public FormResumen()
        {
            InitializeComponent();
            this.Load += FormResumen_Load;
        }

        private void FormResumen_Load(object sender, EventArgs e)
        {
            CargarResumen(DateTime.Today);
        }

        private void CargarResumen(DateTime dia)
        {
            // La base guarda la hora en UTC: convertimos el día argentino a UTC para consultar
            DateTime desde = dia.Date.ToUniversalTime();
            DateTime hasta = dia.Date.AddDays(1).ToUniversalTime();
            lblFecha.Text = dia.Date.ToString("dddd d 'de' MMMM 'de' yyyy");

            Conexion cn = new Conexion();
            try
            {
                MySqlConnection con = cn.AbrirConexion();

                // 1) Total vendido, cantidad de ventas y ticket promedio
                MySqlCommand cmdResumen = new MySqlCommand(
                    @"SELECT COALESCE(SUM(Total), 0) AS TotalVendido,
                             COUNT(*) AS CantidadVentas,
                             COALESCE(AVG(Total), 0) AS TicketPromedio
                      FROM Ventas
                      WHERE Fecha >= @desde AND Fecha < @hasta", con);
                cmdResumen.Parameters.AddWithValue("@desde", desde);
                cmdResumen.Parameters.AddWithValue("@hasta", hasta);

                using (MySqlDataReader lector = cmdResumen.ExecuteReader())
                {
                    if (lector.Read())
                    {
                        lblTotal.Text = Convert.ToDecimal(lector["TotalVendido"]).ToString("C2");
                        lblCantidad.Text = lector["CantidadVentas"].ToString();
                        lblPromedio.Text = Convert.ToDecimal(lector["TicketPromedio"]).ToString("C2");
                    }
                }

                // 2) Top 5 de productos más vendidos
                MySqlCommand cmdTop = new MySqlCommand(
                    @"SELECT p.Nombre AS Producto,
                             SUM(d.Cantidad) AS Unidades,
                             SUM(d.Cantidad * d.PrecioUnitario) AS Total
                      FROM DetalleVenta d
                      JOIN Ventas v    ON v.IdVenta = d.IdVenta
                      JOIN Productos p ON p.IdProductos = d.IdProductos
                      WHERE v.Fecha >= @desde AND v.Fecha < @hasta
                      GROUP BY p.IdProductos, p.Nombre
                      ORDER BY Unidades DESC
                      LIMIT 5", con);
                cmdTop.Parameters.AddWithValue("@desde", desde);
                cmdTop.Parameters.AddWithValue("@hasta", hasta);

                DataTable dt = new DataTable();
                using (MySqlDataAdapter da = new MySqlDataAdapter(cmdTop))
                {
                    da.Fill(dt);
                }
                dgvTop.DataSource = dt;

                if (dgvTop.Columns["Total"] != null)
                    dgvTop.Columns["Total"].DefaultCellStyle.Format = "C2";
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo cargar el resumen: " + ex.Message,
                    "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cn.CerrarConexion();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}