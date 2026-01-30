using System.Collections.Generic;
using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_Ventas
    {
        private readonly CD_Ventas objCapaDato = new CD_Ventas();

        public List<Venta> Listar()
        {
            return objCapaDato.Listar();
        }

        public int Registrar(Venta obj, out string mensaje)
        {
            return objCapaDato.Registrar(obj, out mensaje);
        }

        // 🚀 Nuevo método para el checkout
        public bool RegistrarCompra(int idCliente, EnvioDetalle envio, out string mensaje)
        {
            return objCapaDato.RegistrarCompra(idCliente, envio, out mensaje);
        }

        // Obtener cabecera de una venta
        public Venta ObtenerVenta(int idVenta)
        {
            return objCapaDato.ObtenerVenta(idVenta);
        }

        // Listar detalle de productos de una venta
        public List<DetalleVenta> ListarDetalle(int idVenta)
        {
            return objCapaDato.ListarDetalle(idVenta);
        }

        public bool ActualizarTransaccion(int idVenta, string idTransaccion, out string mensaje)
        {
            return objCapaDato.ActualizarTransaccion(idVenta, idTransaccion, out mensaje);
        }
    }
}
