using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Text.Json;
using CapaEntidad;
using CapaNegocio;
using System.Linq;
using ClosedXML.Excel;
using CapaPresentacionAdmin.Filters;
using System.Globalization;

namespace CapaPresentacionAdmin.Controllers
{
     [SessionAuthorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly CN_Usuarios objUsuario = new CN_Usuarios();
        private readonly CN_Productos objProducto = new CN_Productos();

        // Acción principal: Dashboard
        public ActionResult Index()
        {
            // Obtener reporte fuertemente tipado desde la capa de negocio
            var reporte = new CN_Reporte().ObtenerReporteDashboard();

            // Si por alguna razón el SP no devolvió clientes, fallback a contar desde la lista de clientes activos
            if (reporte == null)
            {
                reporte = new ReporteDashboard();
                reporte.CantidadClientes = new CN_Cliente().Listar().Count(c => c.Activo);
                reporte.CantidadVentas = 0;
                reporte.CantidadProductos = (objProducto.Listar() ?? new List<Producto>()).Count;
            }
            else
            {
                // fallback granular por propiedad
                if (reporte.CantidadClientes == 0)
                    reporte.CantidadClientes = new CN_Cliente().Listar().Count(c => c.Activo);
                if (reporte.CantidadProductos == 0)
                    reporte.CantidadProductos = (objProducto.Listar() ?? new List<Producto>()).Count;
            }

            // Mantener ViewBag por compatibilidad con otras vistas/scripts que pudieran usarlo
            ViewBag.TotalClientes = reporte.CantidadClientes;
            ViewBag.TotalVentas = reporte.CantidadVentas;
            ViewBag.TotalProductos = reporte.CantidadProductos;

            return View(reporte);
        }

        // small helper to preserve previous fallback behavior for products
        private int productosCountFallback(Dictionary<string, int> resumen, int productosCount)
        {
            return (resumen.ContainsKey("TotalProductos") && resumen["TotalProductos"] > 0) ? resumen["TotalProductos"] : productosCount;
        }

        [HttpPost]
        public JsonResult ReporteVentas(DateTime? fechaInicio, DateTime? fechaFin, string idTransaccion)
        {
            // Parse date strings from request form if binder didn't parse (input type=date sends yyyy-MM-dd)
            DateTime? fInicio = fechaInicio;
            DateTime? fFin = fechaFin;
            try
            {
                if (!fInicio.HasValue && !string.IsNullOrWhiteSpace(Request.Form["fechaInicio"]))
                {
                    var s = Request.Form["fechaInicio"].ToString();
                    DateTime tmp;
                    if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out tmp)) fInicio = tmp;
                    else if (DateTime.TryParse(s, out tmp)) fInicio = tmp;
                }

                if (!fFin.HasValue && !string.IsNullOrWhiteSpace(Request.Form["fechaFin"]))
                {
                    var s = Request.Form["fechaFin"].ToString();
                    DateTime tmp;
                    if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out tmp)) fFin = tmp;
                    else if (DateTime.TryParse(s, out tmp)) fFin = tmp;
                }
            }
            catch { }

            if (fInicio.HasValue && fFin.HasValue && fInicio > fFin)
            {
                return Json(new { data = new List<object>(), error = "Rango de fechas inválido" }, JsonRequestBehavior.AllowGet);
            }

            var lista = new CN_Reporte().ReporteVentas(fInicio, fFin, idTransaccion);
            return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Usuarios()
        {
            var listaUsuarios = objUsuario.Listar(); // trae clientes o admins según tu lógica
            return View(listaUsuarios);
        }

        [HttpGet]
        public JsonResult ListarUsuarios()
        {
            var lista = objUsuario.Listar(); // CN_Usuarios.Listar()
            return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GuardarUsuario(Usuario usuario)
        {
            bool resultado;
            string mensaje;

            if (usuario.IdUsuario == 0)
            {
                // Crear nuevo
                int idGenerado = objUsuario.Registrar(usuario, out mensaje);
                resultado = idGenerado > 0;
                return Json(new { resultado = idGenerado, mensaje = mensaje }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                // Editar existente
                resultado = objUsuario.Editar(usuario, out mensaje);
                return Json(new { resultado = resultado, mensaje = mensaje }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult EliminarUsuario(int idusuario)
        {
            bool resultado;
            string mensaje;

            resultado = objUsuario.Eliminar(idusuario, out mensaje);
            return Json(new { resultado = resultado, mensaje = mensaje }, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public FileResult ExportarVentas(DateTime? fechaInicio, DateTime? fechaFin, string idTransaccion)
        {
            var lista = new CN_Reporte().ReporteVentas(fechaInicio, fechaFin, idTransaccion);

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("ReporteVentas");

                worksheet.Cell(1, 1).Value = "Fecha Venta";
                worksheet.Cell(1, 2).Value = "Cliente";
                worksheet.Cell(1, 3).Value = "Producto";
                worksheet.Cell(1, 4).Value = "Precio";
                worksheet.Cell(1, 5).Value = "Cantidad";
                worksheet.Cell(1, 6).Value = "Total";
                worksheet.Cell(1, 7).Value = "Id Transacción";

                var headerRange = worksheet.Range(1, 1, 1, 7);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                int fila = 2;
                foreach (var v in lista)
                {
                    worksheet.Cell(fila, 1).Value = v.FechaVenta;
                    worksheet.Cell(fila, 2).Value = v.Cliente;
                    worksheet.Cell(fila, 3).Value = v.Producto;
                    worksheet.Cell(fila, 4).Value = v.Precio;
                    worksheet.Cell(fila, 5).Value = v.Cantidad;
                    worksheet.Cell(fila, 6).Value = v.Total;
                    worksheet.Cell(fila, 7).Value = v.IdTransaccion;
                    fila++;
                }

                worksheet.Columns().AdjustToContents();

                var tableRange = worksheet.Range(1, 1, fila - 1, 7);
                tableRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                tableRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

                worksheet.Column(1).Style.DateFormat.Format = "yyyy-MM-dd";
                worksheet.Column(4).Style.NumberFormat.Format = "$ #,##0.00";
                worksheet.Column(6).Style.NumberFormat.Format = "$ #,##0.00";

                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(),
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                $"ReporteVentas_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }

        // Protección de acceso: solo Admin puede entrar
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
             var role = Session["Role"] as string;
            if (string.IsNullOrWhiteSpace(role) || !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Redirigir al login del área Admin (no a Tienda) para evitar bucles entre aplicaciones
                filterContext.Result = RedirectToAction("Index", "Acceso");
                return;
            }

            // Prevent caching of admin pages to avoid back-button access after logout
            try
            {
                Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
                Response.Cache.SetNoStore();
                Response.Cache.SetExpires(DateTime.UtcNow.AddYears(-1));
            }
            catch { }

            base.OnActionExecuting(filterContext);
        }
    }
}
