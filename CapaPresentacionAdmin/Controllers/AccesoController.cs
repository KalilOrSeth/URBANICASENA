using CapaEntidad;
using CapaNegocio;
using System.Linq;
using System.Web.Mvc;
using CapaDatos;
using System;
using System.Text.RegularExpressions;
using System.Configuration;
using System.IO;
using System.Text;
using System.Web.Configuration;

namespace CapaPresentacionAdmin.Controllers
{
    public class AccesoController : Controller
    {
        private const string SessionUsuarioKey = "Usuario";
        private const string SessionCodigoKey = "CodigoRecuperacion";
        private const string SessionCorreoKey = "CorreoRecuperacion";
        private const string SessionCodigoExpKey = "CodigoRecuperacionExpiracion";
        private const string SessionIntentosKey = "IntentosRecuperacion";
        private const int CodigoValidezMinutos = 15;
        private const int IntentosMaximos = 5;

        [HttpGet]
        public ActionResult Index()
        {
            // Si ya hay usuario en sesión y rol Admin, ir al Dashboard en lugar de mostrar el login
            var usuarioEnSesion = Session[SessionUsuarioKey] as Usuario;
            var role = Session["Role"] as string;
            if (usuarioEnSesion != null && string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Home");
            }

            // Mostrar vista de login (no limpiar sesión aquí, para no romper redirecciones entre apps)
            return View();
        }
        [HttpPost]
        public ActionResult Index(string correo, string clave)
        {
            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(clave))
            {
                ViewBag.Error = "Debe ingresar correo y contraseña";
                return View();
            }

            // Validar login contra DB usando CN_UsuarioAdmin.ValidarLogin (apuntar exclusivamente a UsuarioAdmin)
            var usuario = new CN_UsuarioAdmin().ValidarLogin(correo, clave);
            if (usuario == null)
            {
                ViewBag.Error = "Correo o contraseña incorrectos";
                return View();
            }

            // Usar directamente el Rol que viene de la BD
            var role = usuario.Rol;
            Session["Role"] = role;

            if (!string.IsNullOrEmpty(role) && role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Login válido y es admin: establecer sesión de admin
                Session[SessionUsuarioKey] = usuario;
                Session["NombreUsuario"] = usuario.Nombres + " " + usuario.Apellidos;
                return RedirectToAction("Index", "Home");
            }
            else
            {
                // No debería ocurrir: si pasa, tratamos como error de credenciales
                ViewBag.Error = "Acceso no permitido.";
                return View();
            }
        }



        [HttpPost]
        public ActionResult CerrarSesion()
        {
            try
            {
                Session.Clear();
                Session.Abandon();

                Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
                Response.Cache.SetNoStore();
                Response.Cache.SetExpires(DateTime.UtcNow.AddYears(-1));
            }
            catch { }

            return RedirectToAction("Index", "Acceso");
        }

        // Alias para /Acceso/Reestablecer -> RecuperarClave
        [HttpGet]
        public ActionResult Reestablecer()
        {
            return RedirectToAction("RecuperarClave");
        }

        [HttpPost]
        public ActionResult Reestablecer(string correo)
        {
            // Delegar al método existente RecuperarClave
            return RecuperarClave(correo);
        }

        // GET: Mostrar formulario para solicitar código
        [HttpGet]
        public ActionResult RecuperarClave()
        {
            return View();
        }

        [HttpPost]
        public ActionResult RecuperarClave(string correo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(correo))
                {
                    ViewBag.Error = "Debe ingresar un correo válido.";
                    return View();
                }

                string correoNormalized = correo.Trim().ToLower();

                var usuario = new CN_Usuarios().Listar()
                                 .FirstOrDefault(u => u.Correo != null && u.Correo.Trim().ToLower() == correoNormalized);

                if (usuario == null)
                {
                    ViewBag.Error = "Correo no encontrado";
                    return View();
                }

                // Generar código (6 caracteres alfanuméricos)
                string codigo = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

                // Guardar en sesión con expiración y contador de intentos
                Session[SessionCodigoKey] = codigo;
                Session[SessionCorreoKey] = correoNormalized;
                Session[SessionCodigoExpKey] = DateTime.UtcNow.AddMinutes(CodigoValidezMinutos);
                Session[SessionIntentosKey] = 0;

                // Enviar correo real via CN_Recursos
                bool enviado = CN_Recursos.EnviarCorreo(
                    correoNormalized,
                    "Código de recuperación",
                    $"Tu código de seguridad es: {codigo}"
                );

                // Registro de actividad
                LogRecovery(correoNormalized, "SolicitudCodigo", enviado ? "Enviado" : "Error", enviado ? "OK" : "Error al enviar correo");

                if (enviado)
                {
                    ViewBag.Mensaje = "Se ha enviado un código a su correo.";
                    return View("IngresarCodigo");
                }
                else
                {
                    ViewBag.Error = "Error al enviar correo. Revise App_Data/smtp.log para más detalles.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                // Mejor manejo de excepciones y logging
                LogRecovery(correo ?? "(sin correo)", "SolicitudCodigo", "Exception", ex.Message);
                ViewBag.Error = "Ocurrió un error interno. Intente nuevamente más tarde.";
                return View();
            }
        }



        // GET: Formulario para ingresar código
        [HttpGet]
        public ActionResult IngresarCodigo()
        {
            return View();
        }

        // POST: Validar código
        [HttpPost]
        public ActionResult IngresarCodigo(string codigo)
        {
            try
            {
                if (Session[SessionCodigoKey] == null || Session[SessionCorreoKey] == null)
                {
                    ViewBag.Error = "No hay un proceso de recuperación activo. Inicie nuevamente.";
                    return View();
                }

                var expiracionObj = Session[SessionCodigoExpKey];
                if (expiracionObj == null || !(expiracionObj is DateTime))
                {
                    ViewBag.Error = "Código inválido o sin expiración. Inicie el proceso nuevamente.";
                    return View();
                }

                DateTime expiracion = (DateTime)expiracionObj;
                if (DateTime.UtcNow > expiracion)
                {
                    // Limpiar sesión relacionada
                    Session.Remove(SessionCodigoKey);
                    Session.Remove(SessionCorreoKey);
                    Session.Remove(SessionCodigoExpKey);
                    Session.Remove(SessionIntentosKey);
                    ViewBag.Error = "El código ha expirado. Solicite uno nuevo.";
                    return View();
                }

                string codigoEsperado = Session[SessionCodigoKey].ToString().ToUpper();
                string codigoIngresado = (codigo ?? string.Empty).Trim().ToUpper();

                int intentos = (Session[SessionIntentosKey] != null) ? (int)Session[SessionIntentosKey] : 0;

                if (codigoIngresado == codigoEsperado)
                {
                    // Marcar como validado para permitir cambio de contraseña
                    Session["CodigoValidado"] = true;
                    LogRecovery(Session[SessionCorreoKey].ToString(), "ValidarCodigo", "OK", "Código válido");
                    return RedirectToAction("CambiarClave");
                }

                // Código incorrecto -> incrementar intentos
                intentos++;
                Session[SessionIntentosKey] = intentos;

                LogRecovery(Session[SessionCorreoKey].ToString(), "ValidarCodigo", "Fallido", $"Intento {intentos}");

                if (intentos >= IntentosMaximos)
                {
                    // Bloquear flujo y limpiar
                    Session.Remove(SessionCodigoKey);
                    Session.Remove(SessionCorreoKey);
                    Session.Remove(SessionCodigoExpKey);
                    Session.Remove(SessionIntentosKey);
                    ViewBag.Error = "Ha excedido el número de intentos permitidos. Solicite un nuevo código.";
                    return View();
                }

                ViewBag.Error = "Código incorrecto";
                return View();
            }
            catch (Exception ex)
            {
                LogRecovery("(desconocido)", "ValidarCodigo", "Exception", ex.Message);
                ViewBag.Error = "Error interno al validar el código.";
                return View();
            }
        }

        // GET: Formulario para cambiar clave
        [HttpGet]
        public ActionResult CambiarClave()
        {
            // Permitir acceso solo si el código fue validado
            if (Session["CodigoValidado"] == null || !(Session["CodigoValidado"] is bool) || !(bool)Session["CodigoValidado"]) 
            {
                ViewBag.Error = "No autorizado para cambiar la contraseña. Valide el código primero.";
                return RedirectToAction("RecuperarClave");
            }

            return View();
        }

        // POST: Cambiar clave
        [HttpPost]
        public ActionResult CambiarClave(string nuevaClave, string confirmarClave)
        {
            try
            {
                // 1. Validar que exista un correo en sesión
                if (Session[SessionCorreoKey] == null || Session["CodigoValidado"] == null || !(bool)Session["CodigoValidado"]) 
                {
                    ViewBag.Error = "No hay un correo en sesión o no ha validado el código. Vuelva a iniciar el proceso de recuperación.";
                    return View();
                }

                // 2. Validar que las contraseñas coincidan y cumplan requisitos mínimos
                if (string.IsNullOrWhiteSpace(nuevaClave) || string.IsNullOrWhiteSpace(confirmarClave))
                {
                    ViewBag.Error = "Debe completar ambos campos de contraseña.";
                    return View();
                }

                if (nuevaClave.Length < 6)
                {
                    ViewBag.Error = "La contraseña debe tener al menos 6 caracteres.";
                    return View();
                }

                if (nuevaClave != confirmarClave)
                {
                    ViewBag.Error = "Las contraseñas no coinciden.";
                    return View();
                }

                // 3. Obtener el correo desde la sesión
                string correo = Session[SessionCorreoKey] as string;
                var usuario = new CN_Usuarios().Listar()
                                 .FirstOrDefault(u => u.Correo != null && u.Correo.Trim().ToLower() == correo.Trim().ToLower());

                if (usuario == null)
                {
                    ViewBag.Error = "Usuario no encontrado.";
                    return View();
                }

                // 4. Hashear la nueva contraseña
                string claveHash = CN_Recursos.ConvertirSha256(nuevaClave);
                string mensaje;
                bool ok = new CD_Usuarios().ActualizarClave(usuario.IdUsuario, claveHash, out mensaje);

                // 5. Resultado de la actualización
                if (ok)
                {
                    // Limpiar sesión y marcar éxito
                    Session.Remove(SessionCodigoKey);
                    Session.Remove(SessionCorreoKey);
                    Session.Remove(SessionCodigoExpKey);
                    Session.Remove(SessionIntentosKey);
                    Session.Remove("CodigoValidado");

                    LogRecovery(correo, "CambiarClave", "OK", "Contraseña actualizada");

                    ViewBag.Mensaje = "Contraseña actualizada correctamente.";
                    return View();
                }
                else
                {
                    LogRecovery(correo, "CambiarClave", "Error", mensaje);
                    ViewBag.Error = "Error al actualizar la contraseña: " + mensaje;
                    return View();
                }
            }
            catch (Exception ex)
            {
                LogRecovery("(desconocido)", "CambiarClave", "Exception", ex.Message);
                ViewBag.Error = "Ocurrió un error interno. Intente nuevamente más tarde.";
                return View();
            }
        }

        // Diagnostic: show last lines of smtp.log when debugging/testing
        [HttpGet]
        public ActionResult VerSmtpLog()
        {
            try
            {
                // Only allow when in debug/test mode (controlled by MostrarCodigoPrueba appSetting)
                bool permitir = string.Equals(ConfigurationManager.AppSettings["MostrarCodigoPrueba"], "true", StringComparison.OrdinalIgnoreCase);
                if (!permitir)
                {
                    return new HttpStatusCodeResult(403, "Acceso denegado");
                }

                string dataPath = Server.MapPath("~/App_Data");
                string logFile = Path.Combine(dataPath, "smtp.log");

                if (!System.IO.File.Exists(logFile))
                {
                    return Content("No se encontró App_Data/smtp.log");
                }

                var lines = System.IO.File.ReadAllLines(logFile);
                var tail = lines.Skip(Math.Max(0, lines.Length - 200));
                string content = string.Join(System.Environment.NewLine, tail);
                return Content(content, "text/plain", System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return Content("Error al leer smtp.log: " + ex.Message);
            }
        }

        private void LogRecovery(string correo, string accion, string estado, string detalle)
        {
            try
            {
                string dataPath = Server.MapPath("~/App_Data");
                if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
                string logFile = Path.Combine(dataPath, "recovery.log");

                string line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Correo={correo} Accion={accion} Estado={estado} Detalle={detalle}" + Environment.NewLine;
                System.IO.File.AppendAllText(logFile, line, Encoding.UTF8);
            }
            catch
            {
                // No interrumpir el flujo si el logging falla
            }
        }

        [HttpPost]
        public ActionResult TestSmtp(string correoTest)
        {
            try
            {
                bool permitir = string.Equals(ConfigurationManager.AppSettings["MostrarCodigoPrueba"], "true", StringComparison.OrdinalIgnoreCase);
                if (!permitir)
                {
                    return new HttpStatusCodeResult(403, "Acceso denegado");
                }

                if (string.IsNullOrWhiteSpace(correoTest))
                {
                    return Content("Debe indicar un correo para la prueba.");
                }

                bool enviado = CN_Recursos.EnviarCorreo(correoTest, "Prueba SMTP", "Mensaje de prueba desde la aplicación.");

                // leer log
                string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
                string dataPath = Path.Combine(baseDir, "App_Data");
                string logFile = Path.Combine(dataPath, "smtp.log");

                string logContent = "(no se encontró smtp.log)";
                if (System.IO.File.Exists(logFile))
                {
                    var lines = System.IO.File.ReadAllLines(logFile);
                    var tail = lines.Skip(Math.Max(0, lines.Length - 200));
                    logContent = string.Join(System.Environment.NewLine, tail);
                }

                string result = enviado ? "ENVIADO" : "FALLÓ";
                string response = $"Resultado: {result}\n--- smtp.log (últimas líneas) ---\n{logContent}";
                return Content(response, "text/plain", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return Content("Error al ejecutar prueba SMTP: " + ex.Message);
            }
        }

        // Quick GET test to send SMTP test email and return smtp.log. Enabled only when MostrarCodigoPrueba=true
        [HttpGet]
        public ActionResult TestSmtpGet(string correoTest)
        {
            try
            {
                bool permitir = string.Equals(ConfigurationManager.AppSettings["MostrarCodigoPrueba"], "true", StringComparison.OrdinalIgnoreCase);
                if (!permitir)
                {
                    return new HttpStatusCodeResult(403, "Acceso denegado");
                }

                if (string.IsNullOrWhiteSpace(correoTest))
                {
                    return Content("Debe indicar un correoTest como querystring, p.ej. /Acceso/TestSmtpGet?correoTest=mi@mail.test");
                }

                // Trigger send
                bool enviado = CN_Recursos.EnviarCorreo(correoTest, "Prueba SMTP GET", "Mensaje de prueba desde la aplicación (GET).");

                // Read log
                string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
                string dataPath = Path.Combine(baseDir, "App_Data");
                string logFile = Path.Combine(dataPath, "smtp.log");

                string logContent = "(no se encontró smtp.log)";
                if (System.IO.File.Exists(logFile))
                {
                    var lines = System.IO.File.ReadAllLines(logFile);
                    var tail = lines.Skip(Math.Max(0, lines.Length - 200));
                    logContent = string.Join(System.Environment.NewLine, tail);
                }

                string result = enviado ? "ENVIADO" : "FALLÓ";
                string response = $"Resultado: {result}\n--- smtp.log (últimas líneas) ---\n{logContent}";
                return Content(response, "text/plain", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return Content("Error al ejecutar prueba SMTP GET: " + ex.Message);
            }
        }
    }
}

