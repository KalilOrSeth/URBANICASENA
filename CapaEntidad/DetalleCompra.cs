using System.Collections.Generic;

namespace CapaEntidad
{
    public class DetalleCompra
    {
        public Venta Venta { get; set; }
        public List<DetalleVenta> Detalles { get; set; }
    }

    // Simple ViewModel for compatibility with views that reference it
    public class DetalleCompraViewModel
    {
        public Venta Venta { get; set; }
        public List<DetalleVenta> Detalles { get; set; }
    }
}
