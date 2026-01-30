using CapaEntidad;
using CapaNegocio;
using CapaPresentacionTienda.Filters;
// using CapaPresentacionTienda.Models.Pago; // ❌ No necesario, usar CapaEntidad.EnvioDetalle
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace CapaPresentacionTienda.Controllers
{
    [SessionAuthorize(Roles = "Cliente")]
    public class TiendaController : Controller
    {
        // Validación general: si no hay sesión, redirige al login
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            bool esAnonima = filterContext.ActionDescriptor
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), false)
                .Any();

            if (!esAnonima && Session["Role"] == null)
            {
                filterContext.Result = RedirectToAction("Login", "Acceso");
            }

            base.OnActionExecuting(filterContext);
        }

        // Acción principal: listado de productos (permitir anonymous)
        [AllowAnonymous]
        public ActionResult Index()
        {
            List<Producto> lista = new CN_Productos().Listar();
            ViewBag.Marcas = new CN_Marca().Listar();
            ViewBag.Categorias = new CN_Categoria().Listar();
            ViewBag.CantidadCarrito = Session["Carrito"] != null ? ((List<ItemCarrito>)Session["Carrito"]).Sum(x => x.Cantidad) : 0;
            return View(lista);
        }

        // GET: agregar al carrito vía link - redirige al carrito
        [HttpGet]
        [SessionAuthorize(Roles = "Cliente")]
        public ActionResult AgregarCarrito(int idProducto)
        {
            var cliente = Session["Cliente"] as Cliente;
            if (cliente == null)
            {
                return RedirectToAction("Index", "Acceso");
            }

            string mensaje;
            bool ok = new CN_Carrito().Agregar(cliente.IdCliente, idProducto, 1, out mensaje);

            try
            {
                var lista = new CN_Carrito().Listar(cliente.IdCliente);
                Session["Carrito"] = lista;
            }
            catch
            {
                var carrito = Session["Carrito"] as List<ItemCarrito> ?? new List<ItemCarrito>();
                Producto prod = new CN_Productos().Listar().Find(p => p.IdProducto == idProducto);
                if (prod != null)
                {
                    var existente = carrito.FirstOrDefault(c => c.Producto.IdProducto == idProducto);
                    if (existente != null) existente.Cantidad += 1; else carrito.Add(new ItemCarrito { Producto = prod, Cantidad = 1 });
                    Session["Carrito"] = carrito;
                }
            }

            return RedirectToAction("Carrito");
        }

        // POST: agregar al carrito vía AJAX - devuelve JSON
        [HttpPost]
        [SessionAuthorize(Roles = "Cliente")]
        public JsonResult AgregarCarritoPost(int idProducto)
        {
            try
            {
                var cliente = Session["Cliente"] as Cliente;
                if (cliente == null)
                {
                    return Json(new { respuesta = false, mensaje = "Sesión expirada. Inicia sesión nuevamente.", cantidad = 0 });
                }

                string mensaje;
                bool ok = new CN_Carrito().Agregar(cliente.IdCliente, idProducto, 1, out mensaje);

                var lista = new CN_Carrito().Listar(cliente.IdCliente);
                Session["Carrito"] = lista;

                int cantidadTotal = lista.Sum(x => x.Cantidad);

                return Json(new { respuesta = ok, mensaje = mensaje ?? (ok ? "Producto agregado" : "Error al agregar"), cantidad = cantidadTotal });
            }
            catch (Exception ex)
            {
                return Json(new { respuesta = false, mensaje = ex.Message, cantidad = 0 });
            }
        }

        // Acción para ver el carrito (permitir visitantes)
        [AllowAnonymous]
        public ActionResult Carrito()
        {
            var carrito = Session["Carrito"] as List<ItemCarrito> ?? new List<ItemCarrito>();
            return View(carrito);
        }

        // GET: Checkout - muestra formulario de envío y datos del carrito
        [SessionAuthorize(Roles = "Cliente")]
        public ActionResult Checkout()
        {
            var cliente = Session["Cliente"] as Cliente;
            if (cliente == null)
            {
                return RedirectToAction("Index", "Acceso");
            }

            var carrito = Session["Carrito"] as List<ItemCarrito> ?? new List<ItemCarrito>();
            if (carrito == null || carrito.Count == 0)
            {
                TempData["Error"] = "Tu carrito está vacío. Agrega productos antes de continuar.";
                return RedirectToAction("Carrito");
            }

            // Sincronizar con BD
            try
            {
                var carritoDb = new CN_Carrito().Listar(cliente.IdCliente);
                if (carritoDb == null || carritoDb.Count == 0)
                {
                    TempData["Error"] = "Tu carrito está vacío en la base de datos.";
                    return RedirectToAction("Carrito");
                }
                Session["Carrito"] = carritoDb;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar el carrito: " + ex.Message;
                return RedirectToAction("Carrito");
            }

            // Simplemente mostrar la vista del checkout
            // El pago con PayPal se procesa después de guardar los datos de envío
            return View();
        }

        // ✅ NUEVO MÉTODO: Guardar datos de envío antes de redirigir a PayPal
        [HttpPost]
        [SessionAuthorize(Roles = "Cliente")]
        public JsonResult GuardarDatosEnvio(string departamento, string ciudad, string barrio,
            string direccion, string referencia, string contacto, string telefono, string idDistrito)
        {
            try
            {
                var cliente = Session["Cliente"] as Cliente;
                if (cliente == null)
                {
                    return Json(new { respuesta = false, mensaje = "Sesión expirada. Inicia sesión nuevamente." });
                }

                // Validar que el carrito tenga productos
                var carrito = Session["Carrito"] as List<ItemCarrito> ?? new List<ItemCarrito>();
                if (carrito == null || carrito.Count == 0)
                {
                    return Json(new { respuesta = false, mensaje = "El carrito está vacío." });
                }

                // Crear objeto con los datos de envío
                var envio = new EnvioDetalle
                {
                    Departamento = departamento,
                    Ciudad = ciudad,
                    Barrio = barrio ?? "", // Opcional
                    Direccion = direccion,
                    Referencia = referencia ?? "", // Opcional
                    Contacto = contacto,
                    Telefono = telefono,
                    IdDistrito = idDistrito
                };

                // Guardar en TempData para que PagoController lo use después
                TempData["DatosEnvio"] = envio;

                return Json(new { respuesta = true, mensaje = "Datos guardados correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { respuesta = false, mensaje = "Error al guardar datos: " + ex.Message });
            }
        }

        // Acción para ver compras pasadas (requiere Cliente)
        [SessionAuthorize(Roles = "Cliente")]
        public ActionResult MisCompras()
        {
            var cliente = Session["Cliente"] as Cliente;
            if (cliente == null)
                return RedirectToAction("Index");

            var compras = new CN_Ventas().Listar()
                                         .Where(v => v.IdCliente == cliente.IdCliente)
                                         .OrderByDescending(v => v.IdVenta)
                                         .ToList();

            return View(compras);
        }

        [SessionAuthorize(Roles = "Cliente")]
        public ActionResult DetalleCompra(int idVenta)
        {
            var cliente = Session["Cliente"] as Cliente;
            if (cliente == null)
                return RedirectToAction("Index", "Acceso");

            Venta venta = new CN_Ventas().ObtenerVenta(idVenta);

            if (venta == null || venta.IdCliente != cliente.IdCliente)
            {
                return RedirectToAction("MisCompras");
            }

            List<DetalleVenta> detalle = new CN_Ventas().ListarDetalle(idVenta) ?? new List<DetalleVenta>();

            try
            {
                venta.TotalProducto = detalle != null ? detalle.Sum(d => d.Cantidad) : 0;
            }
            catch
            {
                venta.TotalProducto = 0;
            }

            var model = new CapaEntidad.DetalleCompra
            {
                Venta = venta,
                Detalles = detalle
            };

            return View(model);
        }

        [SessionAuthorize(Roles = "Cliente")]
        public ActionResult Perfil()
        {
            var idCliente = Session["IdCliente"] != null ? (int)Session["IdCliente"] : 0;
            if (idCliente == 0) return RedirectToAction("Index", "Acceso");

            Cliente cliente = new CN_Cliente().ObtenerPorId(idCliente);
            return View(cliente);
        }

        [SessionAuthorize(Roles = "Cliente")]
        public ActionResult EditarPerfil()
        {
            var idCliente = Session["IdCliente"] != null ? (int)Session["IdCliente"] : 0;
            if (idCliente == 0) return RedirectToAction("Index", "Acceso");

            Cliente cliente = new CN_Cliente().ObtenerPorId(idCliente);
            return View(cliente);
        }

        [HttpPost]
        [SessionAuthorize(Roles = "Cliente")]
        public ActionResult EditarPerfil(Cliente model)
        {
            string mensaje;
            bool ok = new CN_Cliente().Editar(model, out mensaje);

            if (ok)
            {
                try
                {
                    var clienteSesion = Session["Cliente"] as Cliente;
                    if (clienteSesion != null)
                    {
                        clienteSesion.Nombres = model.Nombres;
                        clienteSesion.Apellidos = model.Apellidos;
                        Session["Cliente"] = clienteSesion;
                        Session["NombreCliente"] = clienteSesion.Nombres + " " + clienteSesion.Apellidos;
                    }
                }
                catch { }

                TempData["SwalType"] = "success";
                TempData["SwalMessage"] = "Perfil actualizado correctamente.";
                return RedirectToAction("Perfil");
            }
            else
            {
                TempData["SwalType"] = "error";
                TempData["SwalMessage"] = string.IsNullOrWhiteSpace(mensaje) ? "No se pudo actualizar el perfil." : mensaje;
                return View(model);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public JsonResult CambiarCantidadCarrito(int idProducto, int delta)
        {
            var cliente = Session["Cliente"] as Cliente;
            if (cliente == null) return Json(new { ok = false, message = "Sesión expirada. Inicia sesión nuevamente." });

            try
            {
                var cnCarrito = new CN_Carrito();

                if (!cnCarrito.ExisteCarrito(cliente.IdCliente, idProducto))
                {
                    var carritoSession = Session["Carrito"] as List<ItemCarrito> ?? new List<ItemCarrito>();
                    var sessionItem = carritoSession.FirstOrDefault(c => c.Producto.IdProducto == idProducto);
                    if (sessionItem != null)
                    {
                        carritoSession.Remove(sessionItem);
                        Session["Carrito"] = carritoSession;
                    }

                    TempData["Error"] = "El producto no existe en la base de datos.";

                    if (Request.IsAjaxRequest())
                        return Json(new { ok = false, redirect = Url.Action("Carrito"), message = "El producto no existe en la base de datos." });

                    return Json(new { ok = false, message = "El producto no existe en la base de datos." });
                }

                string mensajeDb;
                bool dbOk = cnCarrito.ActualizarCantidad(cliente.IdCliente, idProducto, delta, out mensajeDb);
                if (!dbOk)
                {
                    return Json(new { ok = false, message = mensajeDb ?? "No se pudo actualizar la cantidad en la base de datos." });
                }

                var lista = cnCarrito.Listar(cliente.IdCliente);
                Session["Carrito"] = lista;

                var item = lista.FirstOrDefault(c => c.Producto.IdProducto == idProducto);
                if (item == null) return Json(new { ok = false, message = "Producto no encontrado en el carrito." });

                decimal totalItem = item.Producto != null ? item.Producto.Precio * item.Cantidad : 0m;
                decimal totalCarrito = lista.Sum(x => (x.Producto != null ? x.Producto.Precio * x.Cantidad : 0m));

                return Json(new
                {
                    ok = true,
                    cantidad = item.Cantidad,
                    totalItem = totalItem,
                    totalCarrito = totalCarrito
                });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public JsonResult EliminarProductoCarrito(int idProducto)
        {
            var cliente = Session["Cliente"] as Cliente;
            if (cliente == null) return Json(new { ok = false, message = "Sesión expirada. Inicia sesión nuevamente." });

            try
            {
                var cnCarrito = new CN_Carrito();

                if (!cnCarrito.ExisteCarrito(cliente.IdCliente, idProducto))
                {
                    var carritoSession = Session["Carrito"] as List<ItemCarrito> ?? new List<ItemCarrito>();
                    var sessionItem = carritoSession.FirstOrDefault(c => c.Producto.IdProducto == idProducto);
                    if (sessionItem != null)
                    {
                        carritoSession.Remove(sessionItem);
                        Session["Carrito"] = carritoSession;
                    }

                    TempData["Error"] = "El producto no existe en la base de datos.";

                    if (Request.IsAjaxRequest())
                        return Json(new { ok = false, redirect = Url.Action("Carrito"), message = "El producto no existe en la base de datos." });

                    return Json(new { ok = false, message = "El producto no existe en la base de datos." });
                }

                string mensajeDb;
                bool dbOk = cnCarrito.Eliminar(cliente.IdCliente, idProducto, out mensajeDb);
                if (!dbOk)
                {
                    return Json(new { ok = false, message = mensajeDb ?? "No se pudo eliminar el producto en la base de datos." });
                }

                var lista = cnCarrito.Listar(cliente.IdCliente);
                Session["Carrito"] = lista;

                decimal totalCarrito = lista.Sum(x => (x.Producto != null ? x.Producto.Precio * x.Cantidad : 0m));
                int count = lista.Sum(x => x.Cantidad);

                return Json(new
                {
                    ok = true,
                    totalCarrito = totalCarrito,
                    count = count
                });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public ContentResult ListarCategorias()
        {
            try
            {
                var lista = new CN_Categoria().Listar();
                string json = JsonConvert.SerializeObject(new { data = lista });
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return Content(JsonConvert.SerializeObject(new { data = new object[0], error = ex.Message }), "application/json");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public ContentResult ListarMarcas()
        {
            try
            {
                var lista = new CN_Marca().Listar().Where(m => m.Activo).OrderBy(m => m.Descripcion).ToList();
                string json = JsonConvert.SerializeObject(new { data = lista });
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return Content(JsonConvert.SerializeObject(new { data = new object[0], error = ex.Message }), "application/json");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public ContentResult ListarMarcasPorCategoria(int idCategoria = 0)
        {
            try
            {
                var productos = new CN_Productos().Listar();
                var marcas = productos
                    .Where(p => p.Activo && p.Stock > 0)
                    .Where(p => idCategoria == 0 || (p.oCategoria != null && p.oCategoria.IdCategoria == idCategoria))
                    .Where(p => p.oMarca != null)
                    .Select(p => new { p.oMarca.IdMarca, p.oMarca.Descripcion })
                    .Distinct()
                    .OrderBy(m => m.Descripcion)
                    .ToList();

                string json = JsonConvert.SerializeObject(new { data = marcas });
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return Content(JsonConvert.SerializeObject(new { data = new object[0], error = ex.Message }), "application/json");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public ContentResult ListarProductos(int idCategoria = 0, int idMarca = 0)
        {
            try
            {
                var productos = new CN_Productos().Listar();
                var filtrados = productos.Where(p =>
                    p.Activo && p.Stock > 0 &&
                    (idCategoria == 0 || (p.oCategoria != null && p.oCategoria.IdCategoria == idCategoria)) &&
                    (idMarca == 0 || (p.oMarca != null && p.oMarca.IdMarca == idMarca))
                ).ToList();

                var lista = filtrados.Select(p => new
                {
                    p.IdProducto,
                    p.Nombre,
                    p.Precio,
                    p.NombreImagen,
                    p.RutaImagen
                });

                string json = JsonConvert.SerializeObject(new { data = lista });
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return Content(JsonConvert.SerializeObject(new { data = new object[0], error = ex.Message }), "application/json");
            }
        }

        [AllowAnonymous]
        public ActionResult ImagenProducto(int id)
        {
            try
            {
                var producto = new CN_Productos().ObtenerPorId(id);
                if (producto == null)
                    return HttpNotFound();

                var virtualPath = (producto.RutaImagen ?? "") + (producto.NombreImagen ?? "");
                var physicalPath = Server.MapPath(virtualPath);
                if (!System.IO.File.Exists(physicalPath))
                {
                    physicalPath = Server.MapPath("~/Content/img/productos/default.jpg");
                }

                var ext = Path.GetExtension(physicalPath).ToLowerInvariant();
                string contentType = "image/jpeg";
                if (ext == ".png") contentType = "image/png";
                else if (ext == ".gif") contentType = "image/gif";

                return File(physicalPath, contentType);
            }
            catch
            {
                return File(Server.MapPath("~/Content/img/productos/default.jpg"), "image/jpeg");
            }
        }

        [AllowAnonymous]
        public ActionResult DetalleProducto(int? id)
        {
            if (id == null)
                return RedirectToAction("Index");

            var producto = new CN_Productos().ObtenerPorId(id.Value);
            if (producto == null)
                return RedirectToAction("Index");

            return View(producto);
        }

        [HttpGet]
        [SessionAuthorize(Roles = "Cliente")]
        public JsonResult CantidadEnCarrito()
        {
            var cliente = Session["Cliente"] as Cliente;
            if (cliente == null)
                return Json(new { cantidad = 0 }, JsonRequestBehavior.AllowGet);

            int cantidad = new CN_Carrito().CantidadEnCarrito(cliente.IdCliente);
            return Json(new { cantidad }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [SessionAuthorize(Roles = "Cliente")]
        public JsonResult ExisteEnCarrito(int idProducto)
        {
            var cliente = Session["Cliente"] as Cliente;
            if (cliente == null)
                return Json(new { existe = false }, JsonRequestBehavior.AllowGet);

            bool existe = new CN_Carrito().ExisteCarrito(cliente.IdCliente, idProducto);
            return Json(new { existe }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [SessionAuthorize(Roles = "Cliente")]
        public JsonResult ConfirmarCompra(string departamento, string ciudad, string barrio, string direccion, string referencia,
                              string contacto, string telefono, string idDistrito, string idTransaccion)
        {
            try
            {
                var cliente = Session["Cliente"] as Cliente;
                if (cliente == null)
                    return Json(new { respuesta = false, mensaje = "Sesión expirada. Inicia sesión nuevamente." });

                var carrito = Session["Carrito"] as List<ItemCarrito> ?? new List<ItemCarrito>();
                if (carrito == null || carrito.Count == 0)
                    return Json(new { respuesta = false, mensaje = "El carrito está vacío." });

                int cantidadEnBD = new CN_Carrito().CantidadEnCarrito(cliente.IdCliente);
                if (cantidadEnBD <= 0)
                {
                    var msg = "Tu carrito está vacío en la base de datos, agrega productos antes de confirmar.";
                    return Json(new { respuesta = false, mensaje = msg, redirect = Url.Action("Carrito") });
                }

                var envio = new EnvioDetalle
                {
                    Departamento = departamento,
                    Ciudad = ciudad,
                    Barrio = barrio,
                    Direccion = direccion,
                    Referencia = referencia,
                    Contacto = contacto,
                    Telefono = telefono,
                    IdDistrito = idDistrito,
                    IdTransaccion = idTransaccion
                };

                string mensaje;
                bool ok = new CN_Ventas().RegistrarCompra(cliente.IdCliente, envio, out mensaje);

                if (!ok)
                {
                    return Json(new { respuesta = false, mensaje });
                }

                // Vaciar el carrito después de la compra exitosa
                new CN_Carrito().Vaciar(cliente.IdCliente, out _);
                Session["Carrito"] = new List<ItemCarrito>();

                var confirmation = "Tu compra ha sido confirmada. ¡Gracias por tu compra!";
                return Json(new { respuesta = true, mensaje = confirmation, redirect = Url.Action("MisCompras") });
            }
            catch (Exception ex)
            {
                return Json(new { respuesta = false, mensaje = "Error interno: " + ex.Message });
            }
        }

        // Métodos para datos de ubicación
        [HttpGet]
        [AllowAnonymous]
        public JsonResult ObtenerDepartamentos()
        {
            try
            {
                // Opción 1: Si tienes una clase CN_Ubicacion o similar
                // var departamentos = new CN_Ubicacion().ObtenerDepartamentos();

                // Opción 2: Lista hardcoded de departamentos de Colombia
                var departamentos = new List<string>
                {
                    "Amazonas", "Antioquia", "Arauca", "Atlántico", "Bolívar",
                    "Boyacá", "Caldas", "Caquetá", "Casanare", "Cauca",
                    "Cesar", "Chocó", "Córdoba", "Cundinamarca", "Guainía",
                    "Guaviare", "Huila", "La Guajira", "Magdalena", "Meta",
                    "Nariño", "Norte de Santander", "Putumayo", "Quindío", "Risaralda",
                    "San Andrés y Providencia", "Santander", "Sucre", "Tolima", "Valle del Cauca",
                    "Vaupés", "Vichada", "Bogotá D.C."
                };

                return Json(new { data = departamentos.OrderBy(d => d).ToList() }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<string>(), error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public JsonResult ObtenerCiudades(string departamento)
        {
            try
            {
                if (string.IsNullOrEmpty(departamento))
                {
                    return Json(new { data = new List<string>() }, JsonRequestBehavior.AllowGet);
                }

                // Opción 1: Si tienes una clase CN_Ubicacion
                // var ciudades = new CN_Ubicacion().ObtenerCiudades(departamento);

                // Opción 2: Diccionario con ciudades principales por departamento
                var ciudadesPorDepartamento = new Dictionary<string, List<string>>
                {
                    { "Bogotá D.C.", new List<string> { "Bogotá" } },
                    { "Antioquia", new List<string> { "Medellín", "Bello", "Itagüí", "Envigado", "Apartadó", "Turbo", "Rionegro" } },
                    { "Valle del Cauca", new List<string> { "Cali", "Palmira", "Buenaventura", "Tuluá", "Cartago", "Buga" } },
                    { "Atlántico", new List<string> { "Barranquilla", "Soledad", "Malambo", "Sabanalarga" } },
                    { "Santander", new List<string> { "Bucaramanga", "Floridablanca", "Girón", "Piedecuesta", "Barrancabermeja" } },
                    { "Cundinamarca", new List<string> { "Soacha", "Facatativá", "Chía", "Zipaquirá", "Fusagasugá", "Madrid", "Mosquera", "Funza" } },
                    { "Bolívar", new List<string> { "Cartagena", "Magangué", "Turbaco", "Arjona" } },
                    { "Norte de Santander", new List<string> { "Cúcuta", "Ocaña", "Villa del Rosario", "Pamplona" } },
                    { "Tolima", new List<string> { "Ibagué", "Espinal", "Melgar", "Honda" } },
                    { "Cauca", new List<string> { "Popayán", "Santander de Quilichao", "Puerto Tejada" } }
                };

                if (ciudadesPorDepartamento.ContainsKey(departamento))
                {
                    return Json(new { data = ciudadesPorDepartamento[departamento].OrderBy(c => c).ToList() }, JsonRequestBehavior.AllowGet);
                }

                // Si no está en el diccionario, retornar lista vacía o ciudad por defecto
                return Json(new { data = new List<string> { departamento } }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<string>(), error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public JsonResult ObtenerBarrios(string ciudad)
        {
            try
            {
                if (string.IsNullOrEmpty(ciudad))
                {
                    return Json(new { data = new List<string>() }, JsonRequestBehavior.AllowGet);
                }

                // Opción 1: Si tienes una clase CN_Ubicacion
                // var barrios = new CN_Ubicacion().ObtenerBarrios(ciudad);

                // Opción 2: Diccionario con barrios por ciudad (ejemplo Bogotá)
                var barriosPorCiudad = new Dictionary<string, List<string>>
                {
                    { "Bogotá", new List<string> {
                        "Usaquén", "Chapinero", "Santa Fe", "San Cristóbal", "Usme",
                        "Tunjuelito", "Bosa", "Kennedy", "Fontibón", "Engativá",
                        "Suba", "Barrios Unidos", "Teusaquillo", "Los Mártires", "Antonio Nariño",
                        "Puente Aranda", "La Candelaria", "Rafael Uribe Uribe", "Ciudad Bolívar", "Sumapaz"
                    }},
                    { "Medellín", new List<string> {
                        "El Poblado", "Laureles", "Belén", "Envigado Centro", "Sabaneta"
                    }},
                    { "Cali", new List<string> {
                        "San Fernando", "Granada", "Ciudad Jardín", "Limonar", "Tequendama"
                    }}
                };

                if (barriosPorCiudad.ContainsKey(ciudad))
                {
                    return Json(new { data = barriosPorCiudad[ciudad].OrderBy(b => b).ToList() }, JsonRequestBehavior.AllowGet);
                }

                // Si no hay barrios específicos, retornar lista vacía (es opcional)
                return Json(new { data = new List<string>() }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<string>(), error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}