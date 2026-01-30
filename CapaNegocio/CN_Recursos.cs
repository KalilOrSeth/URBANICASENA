using CapaDatos;
using CapaEntidad;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CapaNegocio
{
    public static class CN_Recursos
    {
        // Genera una clave aleatoria de 8 caracteres
        public static string GenerarClave()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        // Convierte texto a SHA256
        public static string ConvertirSha256(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(texto);
                byte[] hash = sha256.ComputeHash(bytes);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // Enviar correo usando configuración de Web.config
        public static bool EnviarCorreo(string correo, string asunto, string mensaje)
        {
            bool resultado = false;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
            string dataPath = Path.Combine(baseDir, "App_Data");
            if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
            string logFile = Path.Combine(dataPath, "smtp.log");

            void Log(string t)
            {
                try { File.AppendAllText(logFile, $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {t}{Environment.NewLine}", Encoding.UTF8); } catch { }
            }

            Log($"EnviarCorreo: inicio intento para {correo}");

            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("TuApp", ConfigurationManager.AppSettings["FromEmail"]));
                email.To.Add(new MailboxAddress("", correo));
                email.Subject = asunto;
                email.Body = new TextPart("html") { Text = mensaje };

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    client.Connect(
                        ConfigurationManager.AppSettings["SmtpHost"],
                        int.Parse(ConfigurationManager.AppSettings["SmtpPort"]),
                        SecureSocketOptions.StartTls
                    );

                    client.Authenticate(
                        ConfigurationManager.AppSettings["SmtpUser"],
                        ConfigurationManager.AppSettings["SmtpPass"]
                    );

                    client.Send(email);
                    client.Disconnect(true);

                    resultado = true;
                    Log($"EnviarCorreo: enviado correctamente via {ConfigurationManager.AppSettings["SmtpHost"]}:{ConfigurationManager.AppSettings["SmtpPort"]}");
                }
            }
            catch (Exception ex)
            {
                Log("EnviarCorreo Exception: " + ex.Message);
                if (ex.InnerException != null) Log("Inner: " + ex.InnerException.Message);
                resultado = false;
            }

            return resultado;
        }

        // Convierte archivo a Base64
        public static string ConvertirBase64(string ruta, out bool conversion)
        {
            string textoBase64 = string.Empty;
            conversion = true;

            try
            {
                byte[] bytes = File.ReadAllBytes(ruta);
                textoBase64 = Convert.ToBase64String(bytes);
            }
            catch
            {
                conversion = false;
            }

            return textoBase64;
        }

        // Obtiene la extensión de un archivo
        public static string ObtenerExtension(string ruta)
        {
            return Path.GetExtension(ruta);
        }
    }
}
