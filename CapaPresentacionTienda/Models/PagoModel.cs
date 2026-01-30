using CapaEntidad;
using System;

namespace CapaPresentacionTienda.Models.Pago
{
    public class PagoModel
    {
        public int IdVenta { get; set; }
        public decimal MontoTotal { get; set; }
        public string Moneda { get; set; } = "USD";
        public string ReturnUrl { get; set; }
        public string CancelUrl { get; set; }
        public Venta Venta { get; set; }
    }
}