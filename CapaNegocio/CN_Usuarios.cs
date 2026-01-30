using System.Collections.Generic;
using System.Linq;
using CapaDatos;
using CapaEntidad;
using System.Diagnostics;

namespace CapaNegocio
{
    public class CN_Usuarios
    {
        private readonly CD_Usuarios objCapaDato = new CD_Usuarios();

        public List<Usuario> Listar()
        {
            return objCapaDato.Listar();
        }

        // Validar login: obtiene usuario por correo y compara SHA256 de la clave
        public Usuario ValidarLogin(string correo, string clave)
        {
            string correoNormalized = (correo ?? "").Trim().ToLower();

            if (string.IsNullOrWhiteSpace(correoNormalized) || string.IsNullOrEmpty(clave))
                return null;

            // Obtener usuario desde la BD por correo
            var usuario = objCapaDato.ObtenerPorCorreo(correoNormalized);
            if (usuario == null) return null;

            // Comparación exacta del hash almacenado
            string claveHash = CN_Recursos.ConvertirSha256(clave);
            string storedClave = usuario.Clave?.Trim() ?? string.Empty;

            // Registro de depuración para entender por qué falla (solo en desarrollo)
            Debug.WriteLine($"ValidarLogin: correo='{correoNormalized}' storedClave='{storedClave}' claveHash='{claveHash}' Activo={usuario.Activo} Rol='{usuario.Rol}'");

            if (!string.IsNullOrEmpty(storedClave) &&
                string.Equals(storedClave, claveHash, System.StringComparison.OrdinalIgnoreCase) &&
                usuario.Activo)
            {
                return usuario;
            }

            return null;
        }


        public int Registrar(Usuario obj, out string Mensaje)
        {
            Mensaje = string.Empty;

            obj.Nombres = obj.Nombres?.Trim();
            obj.Apellidos = obj.Apellidos?.Trim();
            obj.Correo = obj.Correo?.Trim();

            if (string.IsNullOrWhiteSpace(obj.Nombres))
            {
                Mensaje = "El nombre del usuario no puede ser vacío";
                return 0;
            }
            else if (string.IsNullOrWhiteSpace(obj.Apellidos))
            {
                Mensaje = "El apellido del usuario no puede ser vacío";
                return 0;
            }
            else if (string.IsNullOrWhiteSpace(obj.Correo))
            {
                Mensaje = "El correo del usuario no puede ser vacío";
                return 0;
            }

            // 👇 Asegurar que Rol tenga un valor
            if (string.IsNullOrWhiteSpace(obj.Rol))
            {
                obj.Rol = "Cliente"; // por defecto Cliente
            }

            if (!string.IsNullOrWhiteSpace(obj.Clave))
            {
                obj.Clave = CN_Recursos.ConvertirSha256(obj.Clave);
                int id = objCapaDato.Registrar(obj, out Mensaje);
                obj.Reestablecer = false;
                return id;
            }

            string claveTemporal = CN_Recursos.GenerarClave();
            string asunto = "Creación de cuenta";
            string mensajeCorreo = "<h3>Su cuenta fue creada correctamente</h3><br>" +
                                   "<p>Su contraseña para acceder es <strong>!clave!</strong></p>";
            mensajeCorreo = mensajeCorreo.Replace("!clave!", claveTemporal);

            bool respuesta = CN_Recursos.EnviarCorreo(obj.Correo, asunto, mensajeCorreo);

            if (respuesta)
            {
                obj.Clave = CN_Recursos.ConvertirSha256(claveTemporal);
                int id = objCapaDato.Registrar(obj, out Mensaje);
                obj.Reestablecer = true;
                return id;
            }
            else
            {
                Mensaje = "No se pudo enviar el correo de registro";
                return 0;
            }
        }


        public bool Editar(Usuario obj, out string Mensaje)
        {
            obj.Nombres = obj.Nombres?.Trim();
            obj.Apellidos = obj.Apellidos?.Trim();
            obj.Correo = obj.Correo?.Trim();

            if (obj.IdUsuario <= 0)
            {
                Mensaje = "Debe indicar el usuario a editar";
                return false;
            }
            if (string.IsNullOrWhiteSpace(obj.Nombres))
            {
                Mensaje = "El nombre del usuario no puede ser vacío";
                return false;
            }
            if (string.IsNullOrWhiteSpace(obj.Apellidos))
            {
                Mensaje = "El apellido del usuario no puede ser vacío";
                return false;
            }
            if (string.IsNullOrWhiteSpace(obj.Correo))
            {
                Mensaje = "El correo del usuario no puede ser vacío";
                return false;
            }

            return objCapaDato.Editar(obj, out Mensaje);
        }

        public bool Eliminar(int idusuario, out string Mensaje)
        {
            if (idusuario <= 0)
            {
                Mensaje = "Debe indicar el usuario a eliminar";
                return false;
            }

            return objCapaDato.Eliminar(idusuario, out Mensaje);
        }
    }
}
