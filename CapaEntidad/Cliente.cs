using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class Cliente
    {
        public int IdCliente { get; set; }
        // Nuevo: soporte para IdUsuario cuando la tabla Cliente referencia Usuarios
        public int IdUsuario { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public string Clave { get; set; }
        public bool Reestablecer { get; set; }
        public string ConfirmarClave { get; set; }
        public bool Restablecer { get; set; }
        // Por defecto, un cliente nuevo debe quedar activo        public bool Activo { get; set; } = true;        public DateTime FechaRegistro { get; set; }        public string Rol { get; set; }    }}
