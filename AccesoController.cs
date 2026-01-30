CapaPresentacionTienda\Controllers\AccesoController.cs
using CapaEntidad;
using CapaNegocio;
using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace CapaPresentacionTienda.Controllers
{
    public class AccesoController : Controller
    {
        private const string SessionClienteKey = "Cliente";
        private static readonly string[] AdminEmails = new[]
        {
            "oxxo@gmail.com",
            "quemiraimlp@gmail.com",
            "monica88_464@ginna.org",
            "santiaginog2005@gmail.com",
            "qwert@gmail.com",
            "waaaow@gmail.com"
        };

        // Alias para evitar 404 cuando se redirige a /Acceso/Login
        [HttpGet]
        public ActionResult Login()
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Login(string correo, string clave)
        {
            return RedirectToAction("Index", new { correo, clave });
        }

        [HttpGet]
        public ActionResult Index()
        {
            // Vista de login / registro
            return View();
        }

        [HttpPost]
        public ActionResult Index(string correo, string clave)
        {
            // limpiar sesión previa
            Session[SessionClienteKey] = null;
            Session["Role"] = null;

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(clave))
            {
                ViewBag.Error = "Debe ingresar correo y contraseña";
                return View();
            }

            // Normalizar correo
            var correoNorm = correo.Trim().ToLowerInvariant();

            // Si el correo está en la lista de Admins, autenticamos como Admin (según tu petición: solo basada en correo)
            if (AdminEmails.Contains(correoNorm))
            {
                // Marcar Admin en sesión
                Session["Role"] = "Admin";
                Session["AdminEmail"] = correoNorm;

                // Redirigir al dashboard Admin. Si tu Admin está en un area, utiliza area; aquí se intenta Dashboard en área Admin.
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            // En otro caso, validar como Cliente
            Cliente cliente;
            bool valido = new CN_Cliente().ValidarLogin(correo, clave, out cliente);

            if (!valido)
            {
                ViewBag.Error = "Correo o contraseña incorrectos";
                return View();
            }

            if (cliente.Reestablecer)
            {
                Session["ClienteCodigoPendiente"] = cliente.IdCliente;
                return RedirectToAction("CambiarClave");
            }

            Session[SessionClienteKey] = cliente;
            Session["Role"] = "Cliente";

            return RedirectToAction("Index", "Tienda");
        }

        [HttpPost]
        public ActionResult Registrar(Cliente obj, string confirmarClave)
        {
            if (obj.Clave != confirmarClave)
            {
                ViewBag.Error = "Las contraseñas no coinciden.";
                return View(obj);
            }

            string mensaje;
            int idGenerado = new CN_Cliente().Registrar(obj, out mensaje);

            if (idGenerado > 0)
            {
                ViewBag.Mensaje = "Registro exitoso. Ahora puede iniciar sesión.";
                return RedirectToAction("Index");
            }

            ViewBag.Error = mensaje;
            return View(obj);
        }

        [HttpGet]
        public ActionResult Reestablecer()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Reestablecer(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
            {
                ViewBag.Error = "Ingresa tu correo.";
                return View();
            }

            var correoNorm = correo.Trim().ToLowerInvariant();

            // Si es Admin -> generar código, enviar y registrar log
            if (AdminEmails.Contains(correoNorm))
            {
                string codigo = CN_Recursos.GenerarClave();
                bool enviado = CN_Recursos.EnviarCorreo(
                    correo,
                    "Código de verificación (Admin)",
                    $"Tu código de verificación es: <b>{codigo}</b>"
                );

                if (!enviado)
                {
                    ViewBag.Error = "No se pudo enviar el correo. Revisa la configuración SMTP.";
                    return View();
                }

                // Guardar en sesión info de admin pendiente
                Session["AdminCodigoPendiente"] = correoNorm;
                Session["CodigoVerificacion"] = codigo;

                // Log en App_Data
                TryLogEvent($"[Reestablecer][Admin] {DateTime.Now:u} - Correo: {correoNorm} - Código enviado: {codigo}");

                return RedirectToAction("CambiarClave");
            }

            // Si no es admin, buscar cliente en BD
            var cliente = new CN_Cliente().ObtenerPorCorreo(correo);
            if (cliente == null)
            {
                ViewBag.Error = "El correo no está registrado.";
                return View();
            }

            string codigoCliente = CN_Recursos.GenerarClave();
            bool enviadoCliente = CN_Recursos.EnviarCorreo(
                correo,
                "Código de verificación",
                $"Tu código de verificación es: <b>{codigoCliente}</b>"
            );

            if (!enviadoCliente)
            {
                ViewBag.Error = "No se pudo enviar el correo. Revisa la configuración SMTP.";
                return View();
            }

            Session["ClienteCodigoPendiente"] = cliente.IdCliente;
            Session["CodigoVerificacion"] = codigoCliente;

            TryLogEvent($"[Reestablecer][Cliente] {DateTime.Now:u} - ClienteId: {cliente.IdCliente} - Correo: {correo} - Código enviado: {codigoCliente}");

            return RedirectToAction("CambiarClave");
        }

        [HttpPost]
        public ActionResult CambiarClave(string codigoIngresado, string nuevaClave, string confirmarClave)
        {
            // Si existe flujo de Admin pendiente
            if (Session["AdminCodigoPendiente"] != null)
            {
                string codigoGuardado = Session["CodigoVerificacion"] as string;
                if (codigoIngresado != codigoGuardado)
                {
                    ViewBag.Error = "El código ingresado no es válido.";
                    return View();
                }

                if (string.IsNullOrWhiteSpace(nuevaClave) || string.IsNullOrWhiteSpace(confirmarClave))
                {
                    ViewBag.Error = "Debe completar ambos campos.";
                    return View();
                }

                if (nuevaClave != confirmarClave)
                {
                    ViewBag.Error = "Las contraseñas no coinciden.";
                    return View();
                }

                // Para Admin: no hay DB a actualizar (según tu petición). Registramos el evento y autenticamos como Admin.
                var correoAdmin = Session["AdminCodigoPendiente"] as string;
                TryLogEvent($"[CambiarClave][Admin] {DateTime.Now:u} - Admin: {correoAdmin} - NuevaClave: (oculta)");

                Session.Remove("AdminCodigoPendiente");
                Session.Remove("CodigoVerificacion");

                Session["Role"] = "Admin";
                Session["AdminEmail"] = correoAdmin;

                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            // Flujo Cliente
            if (Session["ClienteCodigoPendiente"] == null)
                return RedirectToAction("Index");

            string codigoGuardadoCliente = Session["CodigoVerificacion"] as string;
            if (codigoIngresado != codigoGuardadoCliente)
            {
                ViewBag.Error = "El código ingresado no es válido.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(nuevaClave) || string.IsNullOrWhiteSpace(confirmarClave))
            {
                ViewBag.Error = "Debe completar ambos campos.";
                return View();
            }

            if (nuevaClave != confirmarClave)
            {
                ViewBag.Error = "Las contraseñas no coinciden.";
                return View();
            }

            int idCliente = (int)Session["ClienteCodigoPendiente"];
            string mensaje;
            bool ok = new CN_Cliente().ActualizarClave(idCliente, nuevaClave, out mensaje);

            if (ok)
            {
                TryLogEvent($"[CambiarClave][Cliente] {DateTime.Now:u} - ClienteId: {idCliente}");

                Session.Remove("ClienteCodigoPendiente");
                Session.Remove("CodigoVerificacion");
                ViewBag.Mensaje = "Contraseña actualizada correctamente.";
                return RedirectToAction("Index");
            }

            ViewBag.Error = mensaje;
            return View();
        }

        [HttpPost]
        public ActionResult CerrarSesion()
        {
            Session.Clear();
            return RedirectToAction("Index");
        }

        private void TryLogEvent(string mensaje)
        {
            try
            {
                var appData = Server.MapPath("~/App_Data");
                if (!Directory.Exists(appData))
                    Directory.CreateDirectory(appData);

                var logFile = Path.Combine(appData, "acceso_logs.txt");
                System.IO.File.AppendAllText(logFile, mensaje + Environment.NewLine);
            }
            catch
            {
                // no lanzar excepción por logging
            }
        }
    }
}