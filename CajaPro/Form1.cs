using CajaPro.Datos;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CajaPro.Logica;
using MySql.Data.MySqlClient;



namespace CajaPro
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void nuevaVentaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormVenta ventanaVenta = new FormVenta();
            ventanaVenta.MdiParent = this;
            ventanaVenta.Show();
        }

        private void productosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormProductos ventanaProd = new FormProductos();
            ventanaProd.MdiParent = this;
            ventanaProd.Show();
        }

        private void clientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormClientes ventanaCli = new FormClientes();
            ventanaCli.MdiParent = this;
            ventanaCli.Show();
        }



        private void AbrirFormInPanel(Form formHijo)
        {
            // Escondemos el dashboard (no lo borramos, para poder volver)
            panel1.Visible = false;

            // Quitamos la pantalla anterior, sin tocar el dashboard
            if (this.panelContenedor.Tag is Form anterior)
            {
                this.panelContenedor.Controls.Remove(anterior);
                anterior.Dispose();
            }

            formHijo.TopLevel = false;
            formHijo.FormBorderStyle = FormBorderStyle.None;
            formHijo.Dock = DockStyle.Fill;

            this.panelContenedor.Controls.Add(formHijo);
            this.panelContenedor.Tag = formHijo;
            formHijo.Show();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            AbrirFormInPanel(new FormVenta());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            AbrirFormInPanel(new FormClientes());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            AbrirFormInPanel(new FormProductos());
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Volver al dashboard: cerramos la pantalla abierta y mostramos el panel
            if (this.panelContenedor.Tag is Form abierto)
            {
                this.panelContenedor.Controls.Remove(abierto);
                abierto.Dispose();
                this.panelContenedor.Tag = null;
            }
            panel1.Visible = true;
            CargarDashboard();
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CargarDashboard();
        }
        private void CargarDashboard()
        {
            DateTime desde = DateTime.Today.ToUniversalTime();
            DateTime hasta = DateTime.Today.AddDays(1).ToUniversalTime();
            const int umbralStockBajo = 5;

            Conexion cn = new Conexion();
            try
            {
                MySqlConnection con = cn.AbrirConexion();

                label5.Text = Escalar(con,
                    "SELECT COUNT(*) FROM Clientes WHERE Estado = 1 AND DNI <> '00000000'",
                    desde, hasta).ToString();

                label6.Text = Escalar(con,
                    "SELECT COUNT(*) FROM Productos WHERE Estado = 1",
                    desde, hasta).ToString();

                label8.Text = Convert.ToDecimal(Escalar(con,
                    "SELECT COALESCE(SUM(Total), 0) FROM Ventas WHERE Fecha >= @desde AND Fecha < @hasta",
                    desde, hasta)).ToString("C2");

                label10.Text = Escalar(con,
                    "SELECT COUNT(*) FROM Productos WHERE Estado = 1 AND Stock <= " + umbralStockBajo,
                    desde, hasta).ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo cargar el dashboard: " + ex.Message,
                    "CajaPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cn.CerrarConexion();
            }
        }

        private object Escalar(MySqlConnection con, string sql, DateTime desde, DateTime hasta)
        {
            MySqlCommand cmd = new MySqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@desde", desde);
            cmd.Parameters.AddWithValue("@hasta", hasta);
            return cmd.ExecuteScalar();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            new FormResumen().ShowDialog();
            CargarDashboard();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            DialogResult r = MessageBox.Show("¿Seguro que querés salir de CajaPro?",
       "CajaPro", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r == DialogResult.Yes)
                Application.Exit();
        }
    }
}