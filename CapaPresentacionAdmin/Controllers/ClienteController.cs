using CapaEntidad;
using CapaNegocio;
using System.Collections.Generic;
using System.Web.Mvc;
using CapaPresentacionAdmin.Filters;

namespace CapaPresentacionAdmin.Controllers
{
    [SessionAuthorize(Roles = "Admin")]
    public class ClienteController : Controller
    {
        private readonly CN_Cliente _cnCliente = new CN_Cliente();

        // Vista principal -> /Cliente/Index
        public ActionResult Index()
        {
            return View("Clientes"); // 👈 devuelve la vista Clientes.cshtml
        }

        // Acción para DataTable (AJAX)
        [HttpGet]
        public JsonResult Listar()
        {
            var lista = _cnCliente.Listar();
            // 👇 devolvemos JSON plano para que DataTable lo consuma con dataSrc: ""
            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        // Acción para ver detalle (GET)
        [HttpGet]
        public ActionResult Detalles(int id)
        {
            var cliente = _cnCliente.ObtenerPorId(id);
            if (cliente == null) return HttpNotFound();
            return View("Detalles", cliente);
        }

        // Acción para eliminar vía AJAX
        [HttpPost]
        public JsonResult Eliminar(int idCliente)
        {
            string mensaje;
            bool ok = _cnCliente.Eliminar(idCliente, out mensaje);

            return Json(new { resultado = ok, mensaje = mensaje }, JsonRequestBehavior.AllowGet);
        }

        // GET: Editar Activo (muestra pequeño formulario)
        [HttpGet]
        public ActionResult EditarActivo(int id)
        {
            var cliente = _cnCliente.ObtenerPorId(id);
            if (cliente == null) return HttpNotFound();
            return View("EditarActivo", cliente);
        }

        // POST: guardar estado Activo
        [HttpPost]
        public JsonResult GuardarActivo(int idCliente, bool activo)
        {
            string mensaje;
            bool ok = _cnCliente.EditarEstadoActivo(idCliente, activo, out mensaje);
            return Json(new { ok = ok, mensaje = mensaje }, JsonRequestBehavior.AllowGet);
        }
    }
}
