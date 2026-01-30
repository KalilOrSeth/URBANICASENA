using System.Collections.Generic;
using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class CN_Marca
    {
        private readonly CD_Marca objCapaDato = new CD_Marca();

        public List<Marca> Listar()
        {
            return objCapaDato.Listar();
        }

        public int Registrar(Marca obj, out string Mensaje)
        {
            Mensaje = string.Empty;
            obj.Descripcion = obj.Descripcion?.Trim();

            if (string.IsNullOrWhiteSpace(obj.Descripcion))
            {
                Mensaje = "La descripción de la marca no puede estar vacía";
                return 0;
            }

            return objCapaDato.Registrar(obj, out Mensaje);
        }

        public bool Editar(Marca obj, out string Mensaje)
        {
            Mensaje = string.Empty;
            obj.Descripcion = obj.Descripcion?.Trim();

            if (obj.IdMarca <= 0)
            {
                Mensaje = "Debe indicar la marca a editar";
                return false;
            }
            if (string.IsNullOrWhiteSpace(obj.Descripcion))
            {
                Mensaje = "La descripción de la marca no puede estar vacía";
                return false;
            }

            return objCapaDato.Editar(obj, out Mensaje);
        }

        public bool Eliminar(int idMarca, out string Mensaje)
        {
            if (idMarca <= 0)
            {
                Mensaje = "Debe indicar la marca a eliminar";
                return false;
            }

            return objCapaDato.Eliminar(idMarca, out Mensaje);
        }
    }
}
