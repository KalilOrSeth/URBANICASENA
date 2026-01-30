using System.Collections.Generic;
using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_Productos
    {
        private readonly CD_Productos objCapaDato = new CD_Productos();

        public List<Producto> Listar()
        {
            return objCapaDato.Listar();
        }

        public Producto ObtenerPorId(int id)
        {
            return objCapaDato.ObtenerPorId(id);
        }

        public int Registrar(Producto obj, out string Mensaje)
        {
            Mensaje = string.Empty;

            // Normaliza entradas
            obj.Nombre = obj.Nombre?.Trim();
            obj.Descripcion = obj.Descripcion?.Trim();

            // Validaciones
            if (string.IsNullOrWhiteSpace(obj.Nombre))
            {
                Mensaje = "El nombre del producto no puede estar vacío";
                return 0;
            }
            if (string.IsNullOrWhiteSpace(obj.Descripcion))
            {
                Mensaje = "La descripción del producto no puede estar vacía";
                return 0;
            }
            // Usar las propiedades de las entidades relacionadas
            if (obj.oCategoria == null || obj.oCategoria.IdCategoria <= 0)
            {
                Mensaje = "Debe seleccionar una categoría válida";
                return 0;
            }
            if (obj.oMarca == null || obj.oMarca.IdMarca <= 0)
            {
                Mensaje = "Debe seleccionar una marca válida";
                return 0;
            }

            // Ejecuta registro
            return objCapaDato.Registrar(obj, out Mensaje);
        }

        public bool Editar(Producto obj, out string Mensaje)
        {
            Mensaje = string.Empty;

            // Normaliza entradas
            obj.Nombre = obj.Nombre?.Trim();
            obj.Descripcion = obj.Descripcion?.Trim();

            // Validaciones
            if (obj.IdProducto <= 0)
            {
                Mensaje = "Debe indicar el producto a editar";
                return false;
            }
            if (string.IsNullOrWhiteSpace(obj.Nombre))
            {
                Mensaje = "El nombre del producto no puede estar vacío";
                return false;
            }
            if (string.IsNullOrWhiteSpace(obj.Descripcion))
            {
                Mensaje = "La descripción del producto no puede estar vacía";
                return false;
            }

            // Ejecuta edición
            return objCapaDato.Editar(obj, out Mensaje);
        }

        public bool Eliminar(int idProducto, out string Mensaje)
        {
            if (idProducto <= 0)
            {
                Mensaje = "Debe indicar el producto a eliminar";
                return false;
            }

            return objCapaDato.Eliminar(idProducto, out Mensaje);
        }

        public bool GuardarDatosImagen(Producto producto, out string mensaje)
        {
            mensaje = string.Empty;
            return objCapaDato.GuardarDatosImagen(
                producto.IdProducto,
                producto.RutaImagen,
                producto.NombreImagen,
                out mensaje
            );
        }

    }
}
