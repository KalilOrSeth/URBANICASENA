// CapaPresentacionTienda/Models/PayPal/RespuestaPayPal.cs
using System;

namespace CapaPresentacionTienda.Models.PayPal
{
    public class RespuestaPayPal
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }
        public string IdTransaccion { get; set; }
        public int IdVenta { get; set; }
    }
}