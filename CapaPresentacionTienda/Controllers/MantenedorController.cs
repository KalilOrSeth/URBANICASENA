using CapaEntidad;
using CapaNegocio;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.Json;

namespace CapaPresentacionTienda.Controllers
{
    public class MantenedorController : Controller
    {
        #region Logging

        private void LogInfo(string message)
        {
            try
            {
                var logDir = Server.MapPath("~/App_Data");
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                var logFile = Path.Combine(logDir, "imagen_producto.log");
                var line = DateTime.UtcNow.ToString("o") + " | " + message + Environment.NewLine;
                System.IO.File.AppendAllText(logFile, line);
            }
            catch
            {
                // No lanzar excepción por logging
            }
        }

        #endregion

        #region Servir Imágenes de Productos

        [AllowAnonymous]
        [HttpGet]
        public ActionResult ImagenProducto(int id)
        {
            try
            {
                if (id <= 0)
                {
                    LogInfo($"ImagenProducto llamado con id inválido: {id}");
                    return ServeDefault();
                }

                var producto = new CN_Productos().Listar().Find(p => p.IdProducto == id);
                if (producto == null)
                {
                    LogInfo($"Producto no encontrado para id: {id}");
                    return ServeDefault();
                }

                string nombreImagen = (producto.NombreImagen ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(nombreImagen))
                {
                    LogInfo($"Producto {id} tiene NombreImagen vacío");
                    return ServeDefault();
                }

                nombreImagen = Path.GetFileName(nombreImagen);

                // Primera opción: ~/Content/imagenes_productos/
                var rutaTienda = "~/Content/imagenes_productos/" + nombreImagen;
                var pathTienda = Server.MapPath(rutaTienda);

                if (System.IO.File.Exists(pathTienda))
                {
                    return ServeImageFile(pathTienda, $"Producto {id} desde Content/imagenes_productos");
                }

                // Segunda opción: ~/Images/Imagenes/
                var rutaImages = "~/Images/Imagenes/" + nombreImagen;
                var pathImages = Server.MapPath(rutaImages);

                if (System.IO.File.Exists(pathImages))
                {
                    return ServeImageFile(pathImages, $"Producto {id} desde Images/Imagenes");
                }

                LogInfo($"No se encontró imagen para producto {id}. Intentado: {pathTienda} y {pathImages}");
                return ServeDefault();
            }
            catch (Exception ex)
            {
                LogInfo($"Error en ImagenProducto para id {id}: {ex.Message}");
                return ServeDefault();
            }
        }

        private ActionResult ServeImageFile(string physicalPath, string logMessage)
        {
            try
            {
                var bytes = System.IO.File.ReadAllBytes(physicalPath);
                var mimeType = System.Web.MimeMapping.GetMimeMapping(physicalPath);

                Response.Cache.SetCacheability(System.Web.HttpCacheability.Public);
                Response.Cache.SetMaxAge(TimeSpan.FromDays(7));
                Response.Cache.SetExpires(DateTime.UtcNow.AddDays(7));

                LogInfo(logMessage);

                return File(bytes, mimeType);
            }
            catch (Exception ex)
            {
                LogInfo($"Error al servir archivo {physicalPath}: {ex.Message}");
                return ServeDefault();
            }
        }

        private ActionResult ServeDefault()
        {
            try
            {
                var candidates = new[]
                {
                    Server.MapPath("~/Content/imagenes_productos/default.jpg"),
                    Server.MapPath("~/Content/imagenes_productos/placeholder.png"),
                    Server.MapPath("~/Images/Imagenes/placeholder.png"),
                    Server.MapPath("~/Content/img/default.jpg"),
                    Server.MapPath("~/Content/img/no-image.png")
                };

                foreach (var candidatePath in candidates)
                {
                    if (System.IO.File.Exists(candidatePath))
                    {
                        var bytes = System.IO.File.ReadAllBytes(candidatePath);
                        var mimeType = System.Web.MimeMapping.GetMimeMapping(candidatePath);

                        Response.Cache.SetCacheability(System.Web.HttpCacheability.Public);
                        Response.Cache.SetMaxAge(TimeSpan.FromDays(7));

                        LogInfo($"Sirviendo imagen por defecto: {candidatePath}");

                        return File(bytes, mimeType);
                    }
                }

                LogInfo("No se encontró ninguna imagen por defecto");
            }
            catch (Exception ex)
            {
                LogInfo($"Error al servir imagen por defecto: {ex.Message}");
            }

            return HttpNotFound();
        }

        #endregion

        #region Diagnóstico de Imágenes

        [AllowAnonymous]
        [HttpGet]
        public ContentResult DiagnosticoImagenes()
        {
            try
            {
                var results = new System.Collections.Generic.List<object>();

                var carpetaTienda = Server.MapPath("~/Content/imagenes_productos/");
                var carpetaImages = Server.MapPath("~/Images/Imagenes/");

                LogInfo($"Diagnóstico - carpetaTienda: {carpetaTienda}");
                LogInfo($"Diagnóstico - carpetaImages: {carpetaImages}");

                InspeccionarCarpeta("/Content/imagenes_productos/", carpetaTienda, results);
                InspeccionarCarpeta("/Images/Imagenes/", carpetaImages, results);

                var response = new
                {
                    fecha = DateTime.UtcNow,
                    totalArchivos = results.Count,
                    archivos = results
                };

                string json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                LogInfo($"Error en DiagnosticoImagenes: {ex.Message}");

                string jsonError = JsonSerializer.Serialize(new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });

                return Content(jsonError, "application/json");
            }
        }

        private void InspeccionarCarpeta(string rutaVirtual, string rutaFisica, System.Collections.Generic.List<object> results)
        {
            try
            {
                if (!Directory.Exists(rutaFisica))
                {
                    LogInfo($"La carpeta no existe: {rutaFisica}");
                    return;
                }

                var archivos = Directory.GetFiles(rutaFisica);
                LogInfo($"Encontrados {archivos.Length} archivos en {rutaFisica}");

                foreach (var archivo in archivos)
                {
                    try
                    {
                        var info = new FileInfo(archivo);
                        results.Add(new
                        {
                            nombre = info.Name,
                            rutaFisica = info.FullName,
                            rutaVirtual = rutaVirtual,
                            fechaCreacion = info.CreationTimeUtc,
                            fechaModificacion = info.LastWriteTimeUtc,
                            tamañoBytes = info.Length,
                            extension = info.Extension
                        });
                    }
                    catch (Exception ex)
                    {
                        LogInfo($"Error al inspeccionar archivo {archivo}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogInfo($"Error al inspeccionar carpeta {rutaFisica}: {ex.Message}");
            }
        }

        #endregion
    }
}