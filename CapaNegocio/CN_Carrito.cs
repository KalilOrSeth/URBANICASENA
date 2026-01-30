using CapaDatos;
using CapaEntidad;
using System;
using System.Collections.Generic;

namespace CapaNegocio
{
    public class CN_Carrito
    {
        private CD_Carrito objCarrito = new CD_Carrito();

        public bool ExisteCarrito(int idCliente, int idProducto)
        {
            return objCarrito.ExisteCarrito(idCliente, idProducto);
        }

        // Updated: OperacionCarrito now accepts cantidad
        public bool OperacionCarrito(int idCliente, int idProducto, int cantidad, out string mensaje)
        {
            return objCarrito.OperacionCarrito(idCliente, idProducto, cantidad, out mensaje);
        }

        public int CantidadEnCarrito(int idCliente)
        {
            return objCarrito.CantidadEnCarrito(idCliente);
        }

        public List<ItemCarrito> Listar(int idCliente)
        {
            return objCarrito.Listar(idCliente);
        }

        public bool Eliminar(int idCliente, int idProducto, out string mensaje)
        {
            return objCarrito.Eliminar(idCliente, idProducto, out mensaje);
        }

        // Nuevo wrapper para ActualizarCantidad con mensaje
        public bool ActualizarCantidad(int idCliente, int idProducto, int delta, out string mensaje)
        {
            return objCarrito.ActualizarCantidad(idCliente, idProducto, delta, out mensaje);
        }

        // 🚀 Nuevo método para vaciar todo el carrito
        public bool Vaciar(int idCliente, out string mensaje)
        {
            return objCarrito.Vaciar(idCliente, out mensaje);
        }

        public bool Agregar(int idCliente, int idProducto, int cantidad, out string mensaje)
        {
            // Delegar al método Agregar del CD_Carrito, que ahora llama al SP centralizado
            return objCarrito.Agregar(idCliente, idProducto, cantidad, out mensaje);
        }

    }
}
