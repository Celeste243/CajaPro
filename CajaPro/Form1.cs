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

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private void AbrirFormInPanel(Form formHijo)
        {
            // 1. Limpiamos el panel si ya tiene otra pantalla abierta
            if (this.panelContenedor.Controls.Count > 0)
            {
                this.panelContenedor.Controls.RemoveAt(0);
            }

            // 2. Configuramos el formulario hijo
            formHijo.TopLevel = false;
            formHijo.FormBorderStyle = FormBorderStyle.None;
            formHijo.Dock = DockStyle.Fill; // Esto hace que se adapte al tamaño real del panel contenedor

            // 3. Lo metemos adentro y lo mostramos
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
            // 1. Cambiamos el estado de la variable de incógnito
            FormVenta.ModoIncognitoActivo = !FormVenta.ModoIncognitoActivo;

            // 2. Cambiamos el texto y color del botón (cambiá "button4" por el nombre real de tu botón si da error)
            if (FormVenta.ModoIncognitoActivo)
            {
                button4.Text = "Modo Visible";
                button4.BackColor = Color.FromArgb(46, 117, 89);
            }
            else
            {
                button4.Text = "Modo Incógnito";
                button4.BackColor = Color.FromArgb(15, 20, 30);
            }

            // 3. Le avisamos a la pantalla de ventas que se actualice si está abierta
            if (this.panelContenedor.Tag is FormVenta formVentaAbierto)
            {
                formVentaAbierto.Refresh();
            }
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }
    }
}