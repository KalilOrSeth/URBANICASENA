using System;

namespace CapaEntidad
{
    [Serializable]
    public class ItemCarrito
    {
        // Producto asociado al ítem del carrito (usa la clase Producto existente en el mismo namespace)
        public Producto Producto { get; set; }

        // Cantidad del producto en el carrito
        public int Cantidad { get; set; }

        // Total calculado (precio * cantidad), seguro ante nulls
        public decimal Total => (Producto?.Precio ?? 0m) * Cantidad;
    }
}

