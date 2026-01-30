using CapaDatos;
using CapaEntidad;
using CapaNegocio;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace CapaPresentacionAdmin.Controllers
{
    public class ProductoController : Controller
    {
        // Use the business layer for products
        private readonly CN_Productos _productoService = new CN_Productos();

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

        // GET: Producto/Index
        public ActionResult Index()
        {
            // Traer todos los productos desde la capa de negocio
            List<Producto> lista = _productoService.Listar();
            return View(lista);
        }

        // GET: Producto/Crear
        public ActionResult Crear()
        {
            return View();
        }

        // POST: Producto/Crear
        [HttpPost]
        public ActionResult Crear(Producto model, HttpPostedFileBase archivoImagen)
        {
            // Server-side validation: NombreImagen must be provided and exist
            if (string.IsNullOrWhiteSpace(model.NombreImagen))
            {
                ModelState.AddModelError("NombreImagen", "La imagen seleccionada no existe en la carpeta de Admin.");
                LogInfo($"Crear: NombreImagen vacío o nulo antes de guardar.");
                return View(model);
            }

            string physicalPath = Server.MapPath("~/Content/imagenes_productos/" + model.NombreImagen);
            if (!System.IO.File.Exists(physicalPath))
            {
                ModelState.AddModelError("NombreImagen", "La imagen seleccionada no existe en la carpeta de Admin.");
                LogInfo($"Crear: Imagen no encontrada. Nombre={model.NombreImagen}, Ruta={physicalPath}");
                return View(model);
            }

            LogInfo($"Crear: Imagen validada correctamente. Nombre={model.NombreImagen}, Ruta={physicalPath}");

            string mensaje;
            int idProducto = _productoService.Registrar(model, out mensaje);

            // If a file was uploaded, keep previous behavior
            if (idProducto > 0 && archivoImagen != null && archivoImagen.ContentLength > 0)
            {
                try
                {
                    bool saved = GuardarImagenProducto(idProducto, archivoImagen, out string msgImg);
                    if (!saved && string.IsNullOrWhiteSpace(mensaje)) mensaje = msgImg;
                }
                catch (Exception ex)
                {
                    mensaje = (string.IsNullOrWhiteSpace(mensaje) ? string.Empty : mensaje + " \n") + "Error al procesar imagen: " + ex.Message;
                }
            }
            else
            {
                // No uploaded file; if a NombreImagen was selected, update DB to reference it
                if (idProducto > 0 && !string.IsNullOrWhiteSpace(model.NombreImagen))
                {
                    try
                    {
                        var p = new Producto
                        {
                            IdProducto = idProducto,
                            NombreImagen = model.NombreImagen,
                            RutaImagen = "/Content/imagenes_productos/"
                        };
                        bool ok = _productoService.GuardarDatosImagen(p, out string dbMsg);
                        if (ok)
                        {
                            LogInfo($"Producto {idProducto} image set to {p.NombreImagen} (no upload)");
                        }
                        else
                        {
                            LogInfo($"GuardarDatosImagen failed for product {idProducto}: {dbMsg}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogInfo($"Error setting image for product {idProducto}: {ex.Message}");
                    }
                }
            }

            // map message to TempData keys for toast display
            if (idProducto > 0)
                TempData["Success"] = string.IsNullOrWhiteSpace(mensaje) ? "Registrado - La imagen del producto se guardó correctamente." : mensaje;
            else
                TempData["Error"] = string.IsNullOrWhiteSpace(mensaje) ? "No se pudo crear el producto." : mensaje;

            return RedirectToAction("Index");
        }

        // GET: Producto/Editar/5
        public ActionResult Editar(int id)
        {
            var producto = _productoService.Listar().Find(p => p.IdProducto == id);
            if (producto == null)
                return HttpNotFound();

            return View(producto);
        }

        // POST: Producto/Editar/5
        [HttpPost]
        public ActionResult Editar(Producto model, HttpPostedFileBase archivoImagen)
        {
            // Server-side validation: NombreImagen must be provided and exist
            if (string.IsNullOrWhiteSpace(model.NombreImagen))
            {
                ModelState.AddModelError("NombreImagen", "La imagen seleccionada no existe en la carpeta de Admin.");
                LogInfo($"Editar: NombreImagen vacío o nulo para producto {model.IdProducto}.");
                return View(model);
            }

            string physicalPath = Server.MapPath("~/Content/imagenes_productos/" + model.NombreImagen);
            if (!System.IO.File.Exists(physicalPath))
            {
                ModelState.AddModelError("NombreImagen", "La imagen seleccionada no existe en la carpeta de Admin.");
                LogInfo($"Editar: Imagen no encontrada. Nombre={model.NombreImagen}, Ruta={physicalPath}");
                return View(model);
            }

            LogInfo($"Editar: Imagen validada correctamente para producto {model.IdProducto}. Nombre={model.NombreImagen}, Ruta={physicalPath}");

            string mensaje;
            bool resultado = _productoService.Editar(model, out mensaje);

            if (resultado)
            {
                if (archivoImagen != null && archivoImagen.ContentLength > 0)
                {
                    try
                    {
                        bool saved = GuardarImagenProducto(model.IdProducto, archivoImagen, out string msgImg);
                        if (!saved && string.IsNullOrWhiteSpace(mensaje)) mensaje = msgImg;
                    }
                    catch (Exception ex)
                    {
                        mensaje = (string.IsNullOrWhiteSpace(mensaje) ? string.Empty : mensaje + " \n") + "Error al procesar imagen: " + ex.Message;
                    }
                }
                else
                {
                    // No uploaded file; if a NombreImagen was selected/changed, update DB
                    if (!string.IsNullOrWhiteSpace(model.NombreImagen))
                    {
                        try
                        {
                            var p = new Producto
                            {
                                IdProducto = model.IdProducto,
                                NombreImagen = model.NombreImagen,
                                RutaImagen = "/Content/imagenes_productos/"
                            };
                            bool ok = _productoService.GuardarDatosImagen(p, out string dbMsg);
                            if (ok)
                            {
                                LogInfo($"Producto {model.IdProducto} image set to {p.NombreImagen} (no upload)");
                            }
                            else
                            {
                                LogInfo($"GuardarDatosImagen failed for product {model.IdProducto}: {dbMsg}");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogInfo($"Error setting image for product {model.IdProducto}: {ex.Message}");
                        }
                    }
                }
            }

            if (resultado)
                TempData["Success"] = string.IsNullOrWhiteSpace(mensaje) ? "Actualizado - Producto actualizado correctamente." : mensaje;
            else
                TempData["Error"] = string.IsNullOrWhiteSpace(mensaje) ? "No se pudo actualizar el producto." : mensaje;

            return RedirectToAction("Index");
        }

        // GET: Producto/Eliminar/5
        public ActionResult Eliminar(int id)
        {
            var producto = _productoService.Listar().Find(p => p.IdProducto == id);
            if (producto == null)
                return HttpNotFound();

            return View(producto);
        }

        // POST: Producto/Eliminar/5
        [HttpPost, ActionName("Eliminar")]
        public ActionResult EliminarConfirmado(int id)
        {
            string mensaje;
            bool resultado = _productoService.Eliminar(id, out mensaje);

            if (resultado)
            {
                var producto = _productoService.Listar().Find(p => p.IdProducto == id);
                if (producto != null && !string.IsNullOrEmpty(producto.NombreImagen) && !string.IsNullOrEmpty(producto.RutaImagen))
                {
                    try
                    {
                        string carpetaAbs = Server.MapPath("~/" + producto.RutaImagen.TrimStart('~', '/'));
                        string rutaCompleta = Path.Combine(carpetaAbs, producto.NombreImagen);

                        if (System.IO.File.Exists(rutaCompleta))
                            System.IO.File.Delete(rutaCompleta);

                        // eliminar también en Tienda
                        try
                        {
                            string rutaTienda = Path.GetFullPath(Path.Combine(Server.MapPath("~/"), "..", "CapaPresentacionTienda", "Content", "img", "productos", producto.NombreImagen));
                            if (System.IO.File.Exists(rutaTienda))
                                System.IO.File.Delete(rutaTienda);
                        }
                        catch { }
                    }
                    catch { }
                }
            }

            if (resultado)
                TempData["Success"] = string.IsNullOrWhiteSpace(mensaje) ? "Eliminado - Producto eliminado correctamente." : mensaje;
            else
                TempData["Error"] = string.IsNullOrWhiteSpace(mensaje) ? "No se pudo eliminar el producto." : mensaje;

            return RedirectToAction("Index");
        }

        // Método auxiliar para guardar imagen de producto
        // Guarda directamente en CapaPresentacionTienda/Content/img/productos/ con nombre único y actualiza BD mediante CN_Productos.GuardarDatosImagen
        private bool GuardarImagenProducto(int idProducto, HttpPostedFileBase archivoImagen, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                string ext = Path.GetExtension(archivoImagen.FileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

                string nombreArchivo = $"prod_{idProducto}_{Guid.NewGuid():N}{ext}";

                // Determinar carpeta física de Tienda y crear si no existe
                string tiendaFolder = Path.GetFullPath(Path.Combine(Server.MapPath("~/"), "..", "CapaPresentacionTienda", "Content", "img", "productos"));
                if (!Directory.Exists(tiendaFolder)) Directory.CreateDirectory(tiendaFolder);

                string destinoFisico = Path.Combine(tiendaFolder, nombreArchivo);

                if (archivoImagen.InputStream.CanSeek) archivoImagen.InputStream.Position = 0;

                // Guardar imagen como JPEG en la carpeta de Tienda
                using (var img = Image.FromStream(archivoImagen.InputStream))
                {
                    var codec = ImageCodecInfo.GetImageDecoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
                    if (codec == null)
                    {
                        img.Save(destinoFisico);
                    }
                    else
                    {
                        var eps = new EncoderParameters(1);
                        eps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L);
                        img.Save(destinoFisico, codec, eps);
                    }
                }

                // Actualizar BD con ruta virtual (público en Tienda)
                Producto p = new Producto
                {
                    IdProducto = idProducto,
                    RutaImagen = "~/Content/img/productos/",
                    NombreImagen = nombreArchivo
                };

                bool ok = _productoService.GuardarDatosImagen(p, out mensaje);

                return ok;
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
                return false;
            }
        }

    }
}
