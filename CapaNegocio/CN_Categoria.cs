using System.Collections.Generic;
using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_Categoria
    {
        private readonly CD_Categoria objCapaDato = new CD_Categoria();

        public List<Categoria> Listar()
        {
            return objCapaDato.Listar();
        }

        public int Registrar(Categoria obj, out string Mensaje)
        {
            Mensaje = string.Empty;

            // Normaliza entradas
            obj.Descripcion = obj.Descripcion?.Trim();

            // Validaciones
            if (string.IsNullOrWhiteSpace(obj.Descripcion))
            {
                Mensaje = "La descripción de la categoría no puede estar vacía";
                return 0;
            }

            // Ejecuta registro
            return objCapaDato.Registrar(obj, out Mensaje);
        }

        public bool Editar(Categoria obj, out string Mensaje)
        {
            Mensaje = string.Empty;

            // Normaliza entradas
            obj.Descripcion = obj.Descripcion?.Trim();

            // Validaciones
            if (obj.IdCategoria <= 0)
            {
                Mensaje = "Debe indicar la categoría a editar";
                return false;
            }
            if (string.IsNullOrWhiteSpace(obj.Descripcion))
            {
                Mensaje = "La descripción de la categoría no puede estar vacía";
                return false;
            }

            // Ejecuta edición
            return objCapaDato.Editar(obj, out Mensaje);
        }

        public bool Eliminar(int idCategoria, out string Mensaje)
        {
            if (idCategoria <= 0)
            {
                Mensaje = "Debe indicar la categoría a eliminar";
                return false;
            }

            return objCapaDato.Eliminar(idCategoria, out Mensaje);
        }
    }
}
