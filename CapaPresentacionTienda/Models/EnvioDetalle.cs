// Crear este archivo en: Models/Pago/EnvioDetalle.cs

using System;

namespace CapaPresentacionTienda.Models.Pago
{
    [Serializable]
    public class EnvioDetalle
    {
        public string IdTransaccion { get; set; }
        public string Departamento { get; set; }
        public string Ciudad { get; set; }
        public string Barrio { get; set; }
        public string Direccion { get; set; }
        public string Referencia { get; set; }
        public string Contacto { get; set; }
        public string Telefono { get; set; }
        public int IdDistrito { get; set; }
    }
}