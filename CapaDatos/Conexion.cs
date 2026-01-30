using System;
using System.Configuration;

namespace CapaDatos
{
    public static class Conexion
    {
        // Lee la cadena 'cn' desde Web.config usando ConfigurationManager
        public static readonly string cn;

        static Conexion()
        {
            try
            {
                var settings = ConfigurationManager.ConnectionStrings["cn"];
                if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
                {
                    throw new ConfigurationErrorsException("Falta la cadena de conexión 'cn' en el archivo de configuración. Verifique Web.config en la aplicación web.");
                }

                cn = settings.ConnectionString;
            }
            catch (Exception ex)
            {
                // Re-lanzar con información clara para debugging
                throw new InvalidOperationException("Error inicializando la conexión a la base de datos: " + ex.Message, ex);
            }
        }
    }
}
