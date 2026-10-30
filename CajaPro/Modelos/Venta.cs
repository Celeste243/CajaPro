using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace CajaPro.Modelos
{
    public class Venta
    {
        public string IdFactura { get; set; }
        public string IdCliente { get; set; }
        public DateTime Fecha { get; set; }
        public List<DetalleVenta> Items { get; set; } = new List<DetalleVenta>();
        public decimal Total { get; set; }
    }
}