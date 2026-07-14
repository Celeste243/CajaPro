using System;
using System.Collections.Generic;
using System.Linq;
using CajaPro.Modelos; // Conecta el motor con tus modelos de CajaPro

namespace CajaPro.Logica
{
    public static class SupermercadoEngine
    {
        // Listas globales que simulan las bases de datos de CajaPro en memoria
        public static List<Cliente> Clientes = new List<Cliente>
        {
            new Cliente { Id = "C0", Nombre = "Consumidor Final", Dni = "00000000", Activo = true },
            new Cliente { Id = "C1", Nombre = "Vera Celeste", Dni = "44511073", Activo = true }
        };

        public static List<Producto> Productos = new List<Producto>
        {
            new Producto { Id = "7790001", Nombre = "Leche Entera 1L", Precio = 1200.00m, Stock = 20, Activo = true },
            new Producto { Id = "7790002", Nombre = "Yerba Mate 1Kg", Precio = 3500.00m, Stock = 15, Activo = true },
            new Producto { Id = "7790003", Nombre = "Galletería Surtida", Precio = 950.00m, Stock = 5, Activo = true }
        };

        public static List<Venta> HistorialVentas = new List<Venta>();

        // LÓGICA DE NEGOCIO: Procesa la venta, controla stock grupal y guarda en historial
        public static void RegistrarVenta(string idCliente, List<DetalleVenta> carrito)
        {
            if (carrito == null || carrito.Count == 0)
                throw new Exception("El carrito está vacío. Ingrese productos.");

            var cliente = Clientes.FirstOrDefault(c => c.Id == idCliente && c.Activo);
            if (cliente == null)
                throw new Exception("Error: El cliente seleccionado no es válido o está inactivo.");

            // Validar stock de todos los artículos de CajaPro antes de tocar las góndolas
            foreach (var item in carrito)
            {
                var prod = Productos.FirstOrDefault(p => p.Id == item.IdProducto && p.Activo);
                if (prod == null)
                    throw new Exception($"El producto '{item.NombreProducto}' no está disponible en las góndolas.");

                if (prod.Stock < item.Cantidad)
                    throw new Exception($"¡Sin Stock suficiente de {prod.Nombre}! Disponible en góndola: {prod.Stock} unidades.");
            }

            // Si todo está OK, descontamos el stock del inventario (Interacción entre Módulos)
            foreach (var item in carrito)
            {
                var prod = Productos.First(p => p.Id == item.IdProducto);
                prod.Stock -= item.Cantidad;
            }

            // Guardamos la factura en el historial local del sistema CajaPro
            var nuevaVenta = new Venta
            {
                IdFactura = "FAC-" + (HistorialVentas.Count + 1).ToString("D6"),
                IdCliente = idCliente,
                Fecha = DateTime.Now,
                Items = carrito,
                Total = carrito.Sum(i => i.Subtotal)
            };

            HistorialVentas.Add(nuevaVenta);
        }
    }
}