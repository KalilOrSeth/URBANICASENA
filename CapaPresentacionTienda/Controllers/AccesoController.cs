using CapaEntidad;
using CapaNegocio;
using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Configuration;
using CapaDatos;

namespace CapaPresentacionTienda.Controllers
{
    public class AccesoController : Controller
    {
        private const string SessionClienteKey = "Cliente";

        private string GetAdminUrl()
        {
            var url = ConfigurationManager.AppSettings["AdminAppUrl"];
            return string.IsNullOrWhiteSpace(url) ? null : url.TrimEnd('/');
        }

        // Alias para evitar 404 cuando se redirige a /Acceso/Login
        [HttpGet]
        public ActionResult Login()
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Login(string correo, string clave)
        {
            // Reuse the same logic as Index POST to ensure consistent session handling for admin/client
            return HandleLoginPost(correo, clave);
        }

        // GET: /Acceso/Index
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Index()
        {
            return View();
        }

        // POST: /Acceso/Index
        [HttpPost]
        [AllowAnonymous]
        public ActionResult Index(string correo, string clave)
        {
            // Use shared handler to ensure admin sessions are set consistently
            return HandleLoginPost(correo, clave);
        }

        private ActionResult HandleLoginPost(string correo, string clave)
        {
            // limpiar sesión previa
            Session[SessionClienteKey] = null;
            Session["Role"] = null;
            Session["Usuario"] = null;
            Session["NombreUsuario"] = null;
            Session["IdCliente"] = null;
            Session["NombreCliente"] = null;
            Session["CorreoCliente"] = null;

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(clave))
            {
                ViewBag.Error = "Debe ingresar correo y contraseña";
                return View("Index");
            }

            var correoNorm = correo.Trim().ToLowerInvariant();

            // 1) Intentar validar contra tabla de admins (UsuarioAdmin) primero
            try
            {
                var usuarioAdmin = new CN_UsuarioAdmin().ValidarLogin(correoNorm, clave);
                if (usuarioAdmin != null)
                {
                    // Credenciales admin válidas: mantener en la tienda pero indicar rol Admin
                    Session["Role"] = "Admin";
                    Session["Usuario"] = usuarioAdmin;
                    Session["NombreUsuario"] = usuarioAdmin.Nombres + " " + usuarioAdmin.Apellidos;

                    // No forzar redirección al panel; permitir que el admin navegue la tienda y mostrar link al panel
                    return RedirectToAction("Index", "Tienda");
                }
            }
            catch (Exception ex)
            {
                // registrar fallo de validación admin y continuar con flujo cliente
                try { System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "access_errors.log"), DateTime.UtcNow.ToString("u") + " - AdminValidate error: " + ex.Message + Environment.NewLine); } catch { }
            }

            // 2) Validación como cliente normal
            var clienteExistente = new CN_Cliente().ObtenerPorCorreo(correoNorm);
            if (clienteExistente == null)
            {
                ViewBag.Error = "Correo o contraseña incorrectos";
                return View("Index");
            }

            Cliente cliente;
            bool valido = new CN_Cliente().ValidarLogin(correo, clave, out cliente);

            if (!valido)
            {
                ViewBag.Error = "Correo o contraseña incorrectos";
                return View("Index");
            }

            if (cliente.Restablecer)
            {
                Session["ClienteCodigoPendiente"] = cliente.IdCliente;
                return RedirectToAction("IngresarCodigo");
            }

            // Guardar datos completos en sesión para cliente
            Session[SessionClienteKey] = cliente;
            Session["Role"] = "Cliente";
            Session["IdCliente"] = cliente.IdCliente;
            Session["NombreCliente"] = cliente.Nombres + " " + cliente.Apellidos;
            Session["CorreoCliente"] = cliente.Correo;

            return RedirectToAction("Index", "Tienda");
        }


        // GET: Registrar (muestra formulario)
        [HttpGet]
        public ActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Registrar(Cliente obj, string confirmarClave)
        {
            if (obj == null)
            {
                ViewBag.Error = "Datos inválidos.";
                return View();
            }

            if (obj.Clave != confirmarClave)
            {
                ViewBag.Error = "Las contraseñas no coinciden.";
                return View(obj);
            }

            // Asegurar que los clientes nuevos se registren como activos
            obj.Activo = true;

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
        public ActionResult Restablecer()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Restablecer(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
            {
                ViewBag.Error = "Ingresa tu correo.";
                return View();
            }

            var correoNorm = correo.Trim().ToLowerInvariant();

            // 🔹 Buscar primero en UsuarioAdmin
            var usuarioAdmin = new CD_UsuarioAdmin().ObtenerPorCorreo(correoNorm);
            if (usuarioAdmin != null)
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

                Session["AdminCodigoPendiente"] = correoNorm;
                Session["CodigoVerificacion"] = codigo;

                TryLogEvent($"[Restablecer][Admin] {DateTime.Now:u} - Correo: {correoNorm} - Código enviado: {codigo}");

                return RedirectToAction("CambiarClave");
            }

            // 🔹 Si no es admin, buscar cliente en BD
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

            TryLogEvent($"[Restablecer][Cliente] {DateTime.Now:u} - ClienteId: {cliente.IdCliente} - Correo: {correo} - Código enviado: {codigoCliente}");

            return RedirectToAction("IngresarCodigo");
        }



        // GET: Mostrar formulario para ingresar código (Tienda)
        [HttpGet]
        public ActionResult IngresarCodigo()
        {
            if (Session["ClienteCodigoPendiente"] == null || Session["CodigoVerificacion"] == null)
                return RedirectToAction("Index");

            return View();
        }

        [HttpPost]
        public ActionResult IngresarCodigo(string codigo)
        {
            if (Session["CodigoVerificacion"] == null)
            {
                ViewBag.Error = "No hay un código pendiente. Inicie el proceso nuevamente.";
                return View();
            }

            var codigoGuardado = (Session["CodigoVerificacion"] as string) ?? string.Empty;
            if (!string.Equals(codigo?.Trim(), codigoGuardado, StringComparison.Ordinal))
            {
                ViewBag.Error = "El código ingresado no es válido.";
                return View();
            }

            // Código válido -> permitir cambiar contraseña
            Session["CodigoValidado"] = true;
            return RedirectToAction("CambiarClave");
        }

        // GET: Mostrar formulario para cambiar clave (muestra formulario con campos para código y nueva contraseña)
        [HttpGet]
        public ActionResult CambiarClave()
        {
            // Admin flow
            if (Session["AdminCodigoPendiente"] != null)
            {
                return View();
            }

            // Cliente flow: permitir sólo si el código fue validado
            if (Session["ClienteCodigoPendiente"] == null || Session["CodigoValidado"] == null || !(Session["CodigoValidado"] is bool) || !(bool)Session["CodigoValidado"])
            {
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        public ActionResult CambiarClave(string codigoIngresado, string nuevaClave, string confirmarClave)
        {
            string mensaje;

            // 🔹 Flujo Admin
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

                var correoAdmin = Session["AdminCodigoPendiente"] as string;
                bool okAdmin = new CD_UsuarioAdmin().CambiarClavePorCorreo(correoAdmin, nuevaClave, out mensaje);

                if (okAdmin)
                {
                    TryLogEvent($"[CambiarClave][Admin] {DateTime.Now:u} - Admin: {correoAdmin}");
                    Session.Remove("AdminCodigoPendiente");
                    Session.Remove("CodigoVerificacion");
                    Session["Role"] = "Admin";
                    Session["AdminEmail"] = correoAdmin;

                    var adminUrl = GetAdminUrl();
                    if (!string.IsNullOrEmpty(adminUrl))
                        return Redirect(adminUrl + "/Home/Index");

                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }

                ViewBag.Error = mensaje;
                return View();
            }

            // 🔹 Flujo Cliente
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
            bool okCliente = new CN_Cliente().ActualizarClave(idCliente, nuevaClave, out mensaje);

            if (okCliente)
            {
                TryLogEvent($"[CambiarClave][Cliente] {DateTime.Now:u} - ClienteId: {idCliente}");
                Session.Remove("ClienteCodigoPendiente");
                Session.Remove("CodigoVerificacion");
                Session.Remove("CodigoValidado");
                ViewBag.Mensaje = "Contraseña actualizada correctamente.";
                return RedirectToAction("Index");
            }

            ViewBag.Error = mensaje;
            return View();
        }



        [HttpPost]
        public ActionResult CerrarSesion()
        {
            try
            {
                // Clear and abandon session to prevent back-button access
                Session.Clear();
                Session.Abandon();

                // Prevent caching
                Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
                Response.Cache.SetNoStore();
                Response.Cache.SetExpires(DateTime.UtcNow.AddYears(-1));
            }
            catch { }

            return RedirectToAction("Index", "Acceso");
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

        [HttpPost]
        public ActionResult EnviarCodigoCambioClave()
        {
            var correo = Session["CorreoCliente"] as string;
            var idCliente = Session["IdCliente"] != null ? (int)Session["IdCliente"] : 0;

            if (string.IsNullOrEmpty(correo) || idCliente == 0)
            {
                TempData["SwalType"] = "error";
                TempData["SwalMessage"] = "No se pudo obtener el correo del cliente.";
                return RedirectToAction("Perfil", "Tienda");
            }

            string codigo = CN_Recursos.GenerarClave();
            bool enviado = CN_Recursos.EnviarCorreo(
                correo,
                "Código para cambiar contraseña",
                $"Tu código de verificación es: <b>{codigo}</b>"
            );

            if (!enviado)
            {
                TempData["SwalType"] = "error";
                TempData["SwalMessage"] = "No se pudo enviar el código. Revisa la configuración SMTP.";
                return RedirectToAction("Perfil", "Tienda");
            }

            Session["CodigoCambioPerfil"] = codigo;
            Session["CodigoCambioPerfilValidado"] = false;

            TryLogEvent($"[EnviarCodigoCambioClave] {DateTime.Now:u} - ClienteId: {idCliente} - Correo: {correo} - Código enviado: {codigo}");

            TempData["SwalType"] = "success";
            TempData["SwalMessage"] = "Código enviado correctamente. Revisa tu correo.";

            return RedirectToAction("Perfil", "Tienda");
        }
        [HttpPost]
        public ActionResult CambiarClaveDesdePerfil(string codigo, string nuevaClave, string confirmarClave)
        {
            var idCliente = Session["IdCliente"] != null ? (int)Session["IdCliente"] : 0;
            var codigoGuardado = Session["CodigoCambioPerfil"] as string;

            if (idCliente == 0 || string.IsNullOrEmpty(codigoGuardado))
            {
                TempData["SwalType"] = "error";
                TempData["SwalMessage"] = "No hay código pendiente. Solicita uno primero.";
                return RedirectToAction("Perfil", "Tienda");
            }

            if (codigo != codigoGuardado)
            {
                TempData["SwalType"] = "error";
                TempData["SwalMessage"] = "El código ingresado no es válido.";
                return RedirectToAction("Perfil", "Tienda");
            }

            if (string.IsNullOrWhiteSpace(nuevaClave) || string.IsNullOrWhiteSpace(confirmarClave))
            {
                TempData["SwalType"] = "error";
                TempData["SwalMessage"] = "Debe completar ambos campos.";
                return RedirectToAction("Perfil", "Tienda");
            }

            if (nuevaClave != confirmarClave)
            {
                TempData["SwalType"] = "error";
                TempData["SwalMessage"] = "Las contraseñas no coinciden.";
                return RedirectToAction("Perfil", "Tienda");
            }

            string mensaje;
            bool ok = new CN_Cliente().ActualizarClave(idCliente, nuevaClave, out mensaje);

            if (ok)
            {
                TryLogEvent($"[CambiarClaveDesdePerfil][Cliente] {DateTime.Now:u} - ClienteId: {idCliente}");

                Session.Remove("CodigoCambioPerfil");
                Session.Remove("CodigoCambioPerfilValidado");

                TempData["SwalType"] = "success";
                TempData["SwalMessage"] = "Contraseña actualizada correctamente.";
            }
            else
            {
                TempData["SwalType"] = "error";
                TempData["SwalMessage"] = string.IsNullOrWhiteSpace(mensaje) ? "No se pudo actualizar la contraseña." : mensaje;
            }

            return RedirectToAction("Perfil", "Tienda");
        }

        [HttpPost]
        public ActionResult EnviarCodigoCambioClaveAjax()
        {
            var correo = Session["CorreoCliente"] as string;
            var idCliente = Session["IdCliente"] != null ? (int)Session["IdCliente"] : 0;

            if (string.IsNullOrEmpty(correo) || idCliente == 0)
            {
                return Json(new { ok = false, message = "No se pudo obtener el correo del cliente." });
            }

            string codigo = CN_Recursos.GenerarClave();
            bool enviado = CN_Recursos.EnviarCorreo(
                correo,
                "Código para cambiar contraseña",
                $"Tu código de verificación es: <b>{codigo}</b>"
            );

            if (!enviado)
            {
                return Json(new { ok = false, message = "No se pudo enviar el código. Revisa la configuración SMTP." });
            }

            Session["CodigoCambioPerfil"] = codigo;
            Session["CodigoCambioPerfilValidado"] = false;

            TryLogEvent($"[EnviarCodigoCambioClaveAjax] {DateTime.Now:u} - ClienteId: {idCliente} - Correo: {correo} - Código enviado: {codigo}");

            return Json(new { ok = true, message = "Código enviado correctamente. Revisa tu correo." });
        }

        [HttpPost]
        public ActionResult CambiarClaveDesdePerfilAjax(string codigo, string nuevaClave, string confirmarClave)
        {
            var idCliente = Session["IdCliente"] != null ? (int)Session["IdCliente"] : 0;
            var codigoGuardado = Session["CodigoCambioPerfil"] as string;

            if (idCliente == 0 || string.IsNullOrEmpty(codigoGuardado))
            {
                return Json(new { ok = false, message = "No hay código pendiente. Solicita uno primero." });
            }

            if (!string.Equals(codigo?.Trim(), codigoGuardado, StringComparison.Ordinal))
            {
                return Json(new { ok = false, message = "El código ingresado no es válido." });
            }

            if (string.IsNullOrWhiteSpace(nuevaClave) || string.IsNullOrWhiteSpace(confirmarClave))
            {
                return Json(new { ok = false, message = "Debe completar ambos campos." });
            }

            if (nuevaClave != confirmarClave)
            {
                return Json(new { ok = false, message = "Las contraseñas no coinciden." });
            }

            string mensaje;
            bool ok = new CN_Cliente().ActualizarClave(idCliente, nuevaClave, out mensaje);

            if (ok)
            {
                TryLogEvent($"[CambiarClaveDesdePerfilAjax][Cliente] {DateTime.Now:u} - ClienteId: {idCliente}");

                Session.Remove("CodigoCambioPerfil");
                Session.Remove("CodigoCambioPerfilValidado");

                return Json(new { ok = true, message = "Contraseña actualizada correctamente." });
            }

            // Ensure proper encoding for ñ
            var errorMsg = string.IsNullOrWhiteSpace(mensaje) ? "No se pudo actualizar la contraseña." : mensaje;
            return Json(new { ok = false, message = errorMsg });
        }

        [HttpPost]
        public ActionResult RegistrarCliente(Cliente obj)
        {
            // Asegurar que los clientes nuevos se registren como activos
            obj.Activo = true;

            string mensaje;
            int idGenerado = new CD_Cliente().RegistrarClienteCompleto(obj, out mensaje);

            if (idGenerado > 0)
            {
                // Registro exitoso: redirigir al login o mostrar mensaje
                ViewBag.Mensaje = "Cliente registrado correctamente.";
                return RedirectToAction("Index", "Acceso");
            }
            else
            {
                // Hubo error: mostrar mensaje
                ViewBag.Error = "Error al registrar cliente: " + mensaje;
                return View(obj);
            }
        }


    }
}
