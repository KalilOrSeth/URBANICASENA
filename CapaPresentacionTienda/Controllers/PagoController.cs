using CapaEntidad;
using CapaNegocio;
using CapaPresentacionTienda.Filters;
//using CapaPresentacionTienda.Models.Pago;
using PayPalCheckoutSdk.Orders;
using PayPalHttp;
using PayPalCheckoutSdk.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CapaPresentacionTienda.Controllers
{
    [SessionAuthorize(Roles = "Cliente")]
    public class PagoController : Controller
    {
        private readonly string clientId = ConfigurationManager.AppSettings["PayPal:ClientId"];
        private readonly string clientSecret = ConfigurationManager.AppSettings["PayPal:ClientSecret"];
        private readonly string mode = ConfigurationManager.AppSettings["PayPal:Mode"];

        private PayPalHttpClient GetPayPalClient()
        {
            PayPalEnvironment environment;
            if (mode == "Live")
            {
                environment = new LiveEnvironment(clientId, clientSecret);
            }
            else
            {
                environment = new SandboxEnvironment(clientId, clientSecret);
            }
            return new PayPalHttpClient(environment);
        }

        // GET: Iniciar proceso de pago
        public async Task<ActionResult> IniciarPago()
        {
            try
            {
                var cliente = Session["Cliente"] as Cliente;
                if (cliente == null)
                {
                    TempData["Error"] = "Sesión expirada. Inicia sesión nuevamente.";
                    return RedirectToAction("Index", "Acceso");
                }

                var carrito = Session["Carrito"] as List<ItemCarrito> ?? new List<ItemCarrito>();
                if (carrito == null || carrito.Count == 0)
                {
                    TempData["Error"] = "Tu carrito está vacío.";
                    return RedirectToAction("Carrito", "Tienda");
                }

                // Calcular total
                decimal montoTotal = carrito.Sum(x => x.Producto.Precio * x.Cantidad);

                // Crear orden de PayPal
                var client = GetPayPalClient();
                var request = new OrdersCreateRequest();
                request.Prefer("return=representation");
                request.RequestBody(new OrderRequest
                {
                    CheckoutPaymentIntent = "CAPTURE",
                    PurchaseUnits = new List<PurchaseUnitRequest>
                    {
                        new PurchaseUnitRequest
                        {
                            AmountWithBreakdown = new AmountWithBreakdown
                            {
                                CurrencyCode = "USD",
                                Value = montoTotal.ToString("F2"),
                                AmountBreakdown = new AmountBreakdown
                                {
                                    ItemTotal = new Money
                                    {
                                        CurrencyCode = "USD",
                                        Value = montoTotal.ToString("F2")
                                    }
                                }
                            }
                        }
                    },
                    ApplicationContext = new ApplicationContext
                    {
                        ReturnUrl = Url.Action("Completada", "Pago", null, Request.Url.Scheme),
                        CancelUrl = Url.Action("Cancelada", "Pago", null, Request.Url.Scheme)
                    }
                });

                var response = await client.Execute(request);
                var order = response.Result<Order>();

                // Guardar el ID de la orden en sesión
                Session["PayPalOrderId"] = order.Id;

                // Buscar el link de aprobación
                var approveLink = order.Links.FirstOrDefault(l => l.Rel == "approve")?.Href;

                if (string.IsNullOrEmpty(approveLink))
                {
                    TempData["Error"] = "No se pudo obtener el enlace de aprobación de PayPal.";
                    return RedirectToAction("Checkout", "Tienda");
                }

                return Redirect(approveLink);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al iniciar el pago: " + ex.Message;
                return RedirectToAction("Checkout", "Tienda");
            }
        }

        // GET: Pago completado (callback de PayPal)
        public async Task<ActionResult> Completada(string token)
        {
            try
            {
                var cliente = Session["Cliente"] as Cliente;
                if (cliente == null)
                {
                    TempData["Error"] = "Sesión expirada. Inicia sesión nuevamente.";
                    return RedirectToAction("Index", "Acceso");
                }

                var orderId = Session["PayPalOrderId"] as string;
                if (string.IsNullOrEmpty(orderId))
                {
                    TempData["Error"] = "No se encontró la orden de PayPal.";
                    return RedirectToAction("Carrito", "Tienda");
                }

                // Capturar el pago
                var client = GetPayPalClient();
                var request = new OrdersCaptureRequest(orderId);
                request.RequestBody(new OrderActionRequest());

                var response = await client.Execute(request);
                var order = response.Result<Order>();

                // Verificar que el pago fue exitoso
                if (order.Status != "COMPLETED")
                {
                    TempData["Error"] = "El pago no se completó correctamente.";
                    return RedirectToAction("Fallida");
                }

                // Obtener ID de transacción
                var capture = order.PurchaseUnits[0].Payments.Captures[0];
                string idTransaccion = capture.Id;

                // Obtener datos de envío de TempData (deberían estar guardados en Checkout)
                var envio = TempData["DatosEnvio"] as EnvioDetalle;
                if (envio == null)
                {
                    // Si no hay datos de envío, crear uno básico
                    envio = new EnvioDetalle
                    {
                        IdTransaccion = idTransaccion,
                        Departamento = "Por definir",
                        Ciudad = "Por definir",
                        Direccion = "Por definir",
                        Contacto = cliente.Nombres + " " + cliente.Apellidos,
                        Telefono = "Por definir"
                    };
                }
                else
                {
                    envio.IdTransaccion = idTransaccion;
                }

                // Registrar la compra en la base de datos
                string mensaje;
                bool ok = new CN_Ventas().RegistrarCompra(cliente.IdCliente, envio, out mensaje);

                if (!ok)
                {
                    TempData["Error"] = "El pago se procesó, pero hubo un error al registrar la compra: " + mensaje;
                    return RedirectToAction("Fallida");
                }

                // Vaciar el carrito
                new CN_Carrito().Vaciar(cliente.IdCliente, out _);
                Session["Carrito"] = new List<ItemCarrito>();
                Session.Remove("PayPalOrderId");

                TempData["Success"] = "¡Pago completado exitosamente! Gracias por tu compra.";
                TempData["IdTransaccion"] = idTransaccion;

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar el pago: " + ex.Message;
                return RedirectToAction("Fallida");
            }
        }

        // GET: Pago cancelado
        public ActionResult Cancelada()
        {
            Session.Remove("PayPalOrderId");
            TempData["Warning"] = "Has cancelado el proceso de pago. Tu carrito sigue disponible.";
            return View();
        }

        // GET: Pago fallido
        public ActionResult Fallida()
        {
            Session.Remove("PayPalOrderId");
            return View();
        }
    }
}