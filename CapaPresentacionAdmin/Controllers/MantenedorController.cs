using CapaDatos;
using CapaEntidad;
using CapaNegocio;
using System;
using System.Configuration;
using System.IO;
using System.Text.Json;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using CapaPresentacionAdmin.Filters;
using System.Linq;

namespace CapaPresentacionAdmin.Controllers
{
    [SessionAuthorize]
    public class MantenedorController : Controller
    {
        private readonly CN_Categoria objCategoria = new CN_Categoria();
        private readonly CN_Marca objMarca = new CN_Marca();
        private readonly CN_Productos objProducto = new CN_Productos();

        // logging helper reused
        private void LogInfo(string message)
        {
            try
            {
                var logDir = Server.MapPath("~/App_Data");
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                var logFile = Path.Combine(logDir, "imagen_producto.log");
                var line = DateTime.UtcNow.ToString("o") + " | " + message + Environment.NewLine;
                System.IO.File.AppendAllText(logFile, line);
            }
            catch { }
        }

        // ================================
        // Vistas principales
        // ================================
        public ActionResult Categoria() => View();
        public ActionResult Marca() => View();
        public ActionResult Producto() => View();

        public ActionResult Index()
        {
            var lista = new CD_Productos().Listar();

            ViewBag.Marcas = objMarca.Listar();
            ViewBag.Categorias = objCategoria.Listar();

            return View(lista);
        }

        // Diagnostic: comprobación rápida de conexión a la BD
        [HttpGet]
        public ContentResult TestDb()
        {
            try
            {
                using (var cn = new SqlConnection(CapaDatos.Conexion.cn))
                {
                    cn.Open();
                    using (var cmd = new SqlCommand("SELECT 1", cn))
                    {
                        var r = cmd.ExecuteScalar();
                        return Content("OK - scalar: " + (r?.ToString() ?? "null"));
                    }
                }
            }
            catch (Exception ex)
            {
                return Content("ERROR: " + ex.Message);
            }
        }

        // ================================
        // CRUD Categorías
        // ================================
        [HttpGet]
        public ContentResult ListarCategorias()
        {
            var lista = objCategoria.Listar();
            string json = JsonSerializer.Serialize(new { data = lista });
            return Content(json, "application/json");
        }

        [HttpPost]
        public ContentResult RegistrarCategoria(CapaEntidad.Categoria obj)
        {
            // Ensure Activo parsed if sent as string
            try
            {
                var actVal = Request.Form["Activo"] ?? Request.Form["cboactivo"];
                if (!string.IsNullOrEmpty(actVal))
                {
                    if (bool.TryParse(actVal.ToString(), out bool parsed)) obj.Activo = parsed;
                    else if (actVal == "1" || actVal == "0") obj.Activo = actVal == "1";
                }
            }
            catch { }

            string mensaje;
            int idGenerado = objCategoria.Registrar(obj, out mensaje);
            string json = JsonSerializer.Serialize(new { resultado = idGenerado > 0, idGenerado, mensaje });
            return Content(json, "application/json");
        }

        [HttpPost]
        public ContentResult EditarCategoria(CapaEntidad.Categoria obj)
        {
            // Ensure Activo parsed if sent as string
            try
            {
                var actVal = Request.Form["Activo"] ?? Request.Form["cboactivo"];
                if (!string.IsNullOrEmpty(actVal))
                {
                    if (bool.TryParse(actVal.ToString(), out bool parsed)) obj.Activo = parsed;
                    else if (actVal == "1" || actVal == "0") obj.Activo = actVal == "1";
                }
            }
            catch { }

            string mensaje;
            bool respuesta = objCategoria.Editar(obj, out mensaje);
            string json = JsonSerializer.Serialize(new { resultado = respuesta, mensaje });
            return Content(json, "application/json");
        }

        [HttpPost]
        public ContentResult EliminarCategoria(int idCategoria)
        {
            string mensaje;
            bool respuesta = objCategoria.Eliminar(idCategoria, out mensaje);
            string json = JsonSerializer.Serialize(new { resultado = respuesta, mensaje });
            return Content(json, "application/json");
        }

        // ================================
        // CRUD Marcas
        // ================================
        [HttpGet]
        public ContentResult ListarMarcas()
        {
            var lista = objMarca.Listar();
            string json = JsonSerializer.Serialize(new { data = lista });
            return Content(json, "application/json");
        }

        [HttpPost]
        public ContentResult RegistrarMarca(CapaEntidad.Marca obj)
        {
            // Ensure Activo parsed if sent as string
            try
            {
                var actVal = Request.Form["Activo"] ?? Request.Form["cboActivo"];
                if (!string.IsNullOrEmpty(actVal))
                {
                    if (bool.TryParse(actVal.ToString(), out bool parsed)) obj.Activo = parsed;
                    else if (actVal == "1" || actVal == "0") obj.Activo = actVal == "1";
                }
            }
            catch { }

            string mensaje;
            int idGenerado = objMarca.Registrar(obj, out mensaje);
            string json = JsonSerializer.Serialize(new { resultado = idGenerado > 0, idGenerado, mensaje });
            return Content(json, "application/json");
        }

        [HttpPost]
        public ContentResult EditarMarca(CapaEntidad.Marca obj)
        {
            // Ensure Activo parsed if sent as string
            try
            {
                var actVal = Request.Form["Activo"] ?? Request.Form["cboActivo"];
                if (!string.IsNullOrEmpty(actVal))
                {
                    if (bool.TryParse(actVal.ToString(), out bool parsed)) obj.Activo = parsed;
                    else if (actVal == "1" || actVal == "0") obj.Activo = actVal == "1";
                }
            }
            catch { }

            string mensaje;
            bool respuesta = objMarca.Editar(obj, out mensaje);
            string json = JsonSerializer.Serialize(new { resultado = respuesta, mensaje });
            return Content(json, "application/json");
        }

        [HttpPost]
        public ContentResult EliminarMarca(int idMarca)
        {
            string mensaje;
            bool respuesta = objMarca.Eliminar(idMarca, out mensaje);
            string json = JsonSerializer.Serialize(new { resultado = respuesta, mensaje });
            return Content(json, "application/json");
        }

        // ================================
        // CRUD Productos
        // ================================
        [HttpGet]
        public ContentResult ListarProductos()
        {
            var lista = objProducto.Listar();
            string json = JsonSerializer.Serialize(new { data = lista });
            return Content(json, "application/json");
        }

        [HttpPost]
        public ContentResult GuardarProducto(string objeto, HttpPostedFileBase archivoImagen)
        {
            string mensaje = "Operación realizada.";
            bool operacionExitosa = true;
            int idGenerado = 0;
            string productoRecibido = null;
            string savedImagePath = null;

            try
            {
                if (string.IsNullOrWhiteSpace(objeto) && Request.Form["objeto"] != null)
                {
                    objeto = Request.Form["objeto"];
                }

                if (archivoImagen == null && Request.Files.Count > 0)
                {
                    HttpPostedFileBase f = null;
                    var byName = Request.Files["archivoImagen"];
                    if (byName != null && byName.ContentLength > 0)
                    {
                        f = byName;
                    }
                    else if (Request.Files.Count > 0 && Request.Files[0] != null && Request.Files[0].ContentLength > 0)
                    {
                        f = Request.Files[0];
                    }

                    if (f != null)
                    {
                        archivoImagen = f;
                    }
                }

                if (string.IsNullOrWhiteSpace(objeto))
                    throw new ArgumentException("No se recibió el objeto 'objeto' en la petición.");

                Producto producto = JsonSerializer.Deserialize<Producto>(objeto);
                productoRecibido = JsonSerializer.Serialize(producto);

                if (producto == null)
                    throw new Exception("Deserialización de 'Producto' resultó en null.");

                if (producto.IdProducto == 0)
                {
                    idGenerado = objProducto.Registrar(producto, out mensaje);
                    producto.IdProducto = idGenerado;
                    if (idGenerado <= 0)
                    {
                        operacionExitosa = false;
                        if (string.IsNullOrWhiteSpace(mensaje)) mensaje = "No se pudo generar Id al registrar el producto.";
                    }
                }
                else
                {
                    operacionExitosa = objProducto.Editar(producto, out mensaje);
                    idGenerado = producto.IdProducto;
                }

                if (idGenerado > 0 && archivoImagen != null && archivoImagen.ContentLength > 0)
                {
                    string extension = Path.GetExtension(archivoImagen.FileName);
                    if (string.IsNullOrWhiteSpace(extension)) extension = ".jpg";
                    string uniqueName = Guid.NewGuid().ToString("N") + extension;

                    try
                    {
                        // Determine Admin physical folder and create if needed (Content/imagenes_productos)
                        string adminFolder = Server.MapPath("~/Content/imagenes_productos/");
                        LogInfo($"Resolved admin folder: {adminFolder}");

                        if (!Directory.Exists(adminFolder))
                        {
                            Directory.CreateDirectory(adminFolder);
                            LogInfo($"Created admin folder: {adminFolder}");
                        }

                        string adminDest = Path.Combine(adminFolder, uniqueName);

                        // Use SaveAs to ensure file is physically written
                        archivoImagen.SaveAs(adminDest);

                        // Confirm file existence and log
                        if (System.IO.File.Exists(adminDest))
                        {
                            savedImagePath = adminDest;
                            LogInfo($"Saved image to admin path: {adminDest}");
                        }
                        else
                        {
                            LogInfo($"Image NOT found after SaveAs at: {adminDest}");
                        }

                        // Optionally copy to Tienda folder for compatibility (non-fatal)
                        try
                        {
                            string tiendaFolder = Path.GetFullPath(Path.Combine(Server.MapPath("~/"), "..", "CapaPresentacionTienda", "Content", "imagenes_productos"));
                            if (!Directory.Exists(tiendaFolder)) Directory.CreateDirectory(tiendaFolder);
                            string tiendaDest = Path.Combine(tiendaFolder, uniqueName);
                            System.IO.File.Copy(adminDest, tiendaDest, true);
                            LogInfo($"Copied image to tienda path: {tiendaDest}");
                        }
                        catch (Exception exCopy)
                        {
                            LogInfo($"Copy to tienda failed: {exCopy.Message}");
                        }

                        // Update DB with public path (no ~)
                        producto.NombreImagen = uniqueName;
                        producto.RutaImagen = "/Content/imagenes_productos/";

                        string dbMsg;
                        bool ok = objProducto.GuardarDatosImagen(producto, out dbMsg);
                        if (!ok)
                        {
                            operacionExitosa = false;
                            mensaje = string.IsNullOrWhiteSpace(dbMsg) ? "La SP de imagen devolvió un resultado negativo." : dbMsg;
                        }
                        else
                        {
                            mensaje = string.IsNullOrWhiteSpace(mensaje) ? "Imagen actualizada correctamente" : mensaje;
                        }
                    }
                    catch (Exception ex)
                    {
                        operacionExitosa = false;
                        mensaje = "Error al guardar la imagen: " + ex.Message;
                        LogInfo($"Error saving image for product {idGenerado}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                operacionExitosa = false;
                mensaje = "Error en el proceso: " + ex.Message + "\n" + ex.StackTrace;
                LogInfo($"Error in GuardarProducto: {ex.Message}");
            }

            string json = JsonSerializer.Serialize(new { operacionExitosa, mensaje, idGenerado, productoRecibido, savedImagePath });
            return Content(json, "application/json");
        }




        // helper to get encoder
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return null;
        }

        [HttpPost]
        public ContentResult EliminarProducto(int idProducto)
        {
            string mensaje;
            bool respuesta = objProducto.Eliminar(idProducto, out mensaje);

            if (respuesta)
            {
                var producto = new CD_Productos().ObtenerPorId(idProducto);
                if (producto != null && !string.IsNullOrEmpty(producto.NombreImagen) && !string.IsNullOrEmpty(producto.RutaImagen))
                {
                    try
                    {
                        // Eliminamos imagen en la carpeta pública de Tienda
                        string rutaTienda = Path.GetFullPath(
                            Path.Combine(Server.MapPath("~/"), "..", "CapaPresentacionTienda", "Content", "img", "productos", producto.NombreImagen)
                        );
                        if (System.IO.File.Exists(rutaTienda))
                            System.IO.File.Delete(rutaTienda);
                    }
                    catch (Exception ex)
                    {
                        mensaje += " | Error al eliminar imagen en Tienda: " + ex.Message;
                    }
                }
            }

            string json = JsonSerializer.Serialize(new { resultado = respuesta, mensaje });
            return Content(json, "application/json");
        }



        // ================================
        // Vistas Create/Edit (opcionales)
        // ================================
        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.Marcas = objMarca.Listar();
            ViewBag.Categorias = objCategoria.Listar();
            return View();
        }

        [HttpPost]
        public ActionResult Create(Producto obj, HttpPostedFileBase archivoImagen)
        {
            string objeto = JsonSerializer.Serialize(obj);
            GuardarProducto(objeto, archivoImagen);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var producto = new CD_Productos().ObtenerPorId(id);

            ViewBag.Marcas = objMarca.Listar();
            ViewBag.Categorias = objCategoria.Listar();

            return View(producto);
        }

        [HttpPost]
        public ActionResult Edit(Producto obj, HttpPostedFileBase archivoImagen)
        {
            string objeto = JsonSerializer.Serialize(obj);
            GuardarProducto(objeto, archivoImagen);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Eliminar(int idProducto)
        {
            string mensaje;
            bool resultado = new CD_Productos().Eliminar(idProducto, out mensaje);

            // Map to TempData keys used by layout for toasts
            if (resultado)
            {
                TempData["Success"] = string.IsNullOrWhiteSpace(mensaje) ? "Eliminado - Producto eliminado correctamente." : mensaje;
            }
            else
            {
                TempData["Error"] = string.IsNullOrWhiteSpace(mensaje) ? "Error al eliminar el producto." : mensaje;
            }

            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult ImagenProducto(int id)
        {
            try
            {
                if (id <= 0)
                {
                    LogInfo($"ImagenProducto called with invalid id: {id}");
                    // serve default
                    var defCandidates = new[] {
                        Server.MapPath("~/Content/imagenes_productos/default.jpg"),
                        Server.MapPath("~/Content/imagenes_productos/placeholder.png"),
                        Server.MapPath("~/Images/Imagenes/placeholder.png")
                    };

                    foreach (var def in defCandidates)
                    {
                        if (System.IO.File.Exists(def))
                        {
                            var bytes = System.IO.File.ReadAllBytes(def);
                            var result = File(bytes, MimeMapping.GetMimeMapping(def));
                            Response.Cache.SetCacheability(HttpCacheability.Public);
                            Response.Cache.SetMaxAge(TimeSpan.FromDays(7));
                            LogInfo($"Served default image for invalid id from: {def}");
                            return result;
                        }
                    }

                    return HttpNotFound();
                }

                var producto = new CD_Productos().ObtenerPorId(id);
                if (producto == null)
                {
                    LogInfo($"Producto not found for id: {id}");
                    goto serve_default;
                }

                var nombre = (producto.NombreImagen ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    LogInfo($"Producto {id} has no NombreImagen");
                    goto serve_default;
                }

                nombre = Path.GetFileName(nombre);

                // Check Content/imagenes_productos in Admin
                var adminPath = Server.MapPath(Path.Combine("~/Content/imagenes_productos/", nombre));
                LogInfo($"Attempting adminPath: {adminPath} - Exists: {System.IO.File.Exists(adminPath)}");
                if (System.IO.File.Exists(adminPath))
                {
                    var bytes = System.IO.File.ReadAllBytes(adminPath);
                    var result = File(bytes, MimeMapping.GetMimeMapping(adminPath));
                    Response.Cache.SetCacheability(HttpCacheability.Public);
                    Response.Cache.SetMaxAge(TimeSpan.FromDays(7));
                    LogInfo($"Served image for product {id} from adminPath");
                    return result;
                }

                // Fallback to Images/Imagenes
                var imagesPath = Server.MapPath(Path.Combine("~/Images/Imagenes/", nombre));
                LogInfo($"Attempting imagesPath: {imagesPath} - Exists: {System.IO.File.Exists(imagesPath)}");
                if (System.IO.File.Exists(imagesPath))
                {
                    var bytes = System.IO.File.ReadAllBytes(imagesPath);
                    var result = File(bytes, MimeMapping.GetMimeMapping(imagesPath));
                    Response.Cache.SetCacheability(HttpCacheability.Public);
                    Response.Cache.SetMaxAge(TimeSpan.FromDays(7));
                    LogInfo($"Served image for product {id} from imagesPath");
                    return result;
                }

            serve_default:
                // Serve default candidate
                var fallbackCandidates = new[] {
                    Server.MapPath("~/Content/imagenes_productos/default.jpg"),
                    Server.MapPath("~/Content/imagenes_productos/placeholder.png"),
                    Server.MapPath("~/Images/Imagenes/placeholder.png")
                };

                foreach (var def in fallbackCandidates)
                {
                    LogInfo($"Checking fallback: {def} - Exists: {System.IO.File.Exists(def)}");
                    if (System.IO.File.Exists(def))
                    {
                        var bytes = System.IO.File.ReadAllBytes(def);
                        var result = File(bytes, MimeMapping.GetMimeMapping(def));
                        Response.Cache.SetCacheability(HttpCacheability.Public);
                        Response.Cache.SetMaxAge(TimeSpan.FromDays(7));
                        LogInfo($"Served fallback image from: {def} for product id {id}");
                        return result;
                    }
                }

                LogInfo($"No image found for product {id} and no fallback available");
                return HttpNotFound();
            }
            catch (Exception ex)
            {
                LogInfo($"Error in ImagenProducto for id {id}: {ex.Message}");
                return new HttpStatusCodeResult(500, ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public ContentResult DiagnosticoImagenes()
        {
            try
            {
                var results = new System.Collections.Generic.List<object>();

                // Paths to inspect
                var adminFolder = Server.MapPath("~/Content/imagenes_productos/");
                var imagesFolder = Server.MapPath("~/Images/Imagenes/");

                LogInfo($"Diagnostico: inspecting adminFolder={adminFolder}");
                LogInfo($"Diagnostico: inspecting imagesFolder={imagesFolder}");

                void InspectFolder(string virtualPath, string physicalPath)
                {
                    try
                    {
                        if (!Directory.Exists(physicalPath))
                        {
                            LogInfo($"Folder does not exist: {physicalPath}");
                            return;
                        }

                        var files = Directory.GetFiles(physicalPath);
                        LogInfo($"Found {files.Length} files in {physicalPath}");

                        foreach (var f in files)
                        {
                            try
                            {
                                var fi = new FileInfo(f);
                                results.Add(new
                                {
                                    Nombre = fi.Name,
                                    RutaFisica = fi.FullName,
                                    FechaCreacion = fi.CreationTimeUtc,
                                    FechaModificacion = fi.LastWriteTimeUtc,
                                    TamanoBytes = fi.Length,
                                    CarpetaVirtual = virtualPath
                                });
                            }
                            catch (Exception exF)
                            {
                                LogInfo($"Error inspecting file {f}: {exF.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogInfo($"Error inspecting folder {physicalPath}: {ex.Message}");
                    }
                }

                InspectFolder("/Content/imagenes_productos/", adminFolder);
                InspectFolder("/Images/Imagenes/", imagesFolder);

                string json = JsonSerializer.Serialize(new { inspected = DateTime.UtcNow, count = results.Count, files = results });
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                LogInfo($"Error in DiagnosticoImagenes: {ex.Message}");
                string json = JsonSerializer.Serialize(new { error = ex.Message });
                return Content(json, "application/json");
            }
        }
    }
}
