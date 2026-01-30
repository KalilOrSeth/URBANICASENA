using CapaEntidad;
using CapaNegocio;
using PayPalCheckoutSdk.Orders;
using PayPalHttp;
using PayPalCheckoutSdk.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Configuration;
using CapaPresentacionTienda.Filters;

namespace CapaPresentacionTienda.Controllers
{
    [SessionAuthorize]
    public class PayPalController : Controller
    {
        // Configuración de PayPal
        private readonly string clientId = ConfigurationManager.AppSettings["PayPal:ClientId"];
        private readonly string clientSecret = ConfigurationManager.AppSettings["PayPal:ClientSecret"];
        private readonly string mode = ConfigurationManager.AppSettings["PayPal:Mode"];

        // Crear pedido de pago
        public async Task<ActionResult> CrearPago(int idVenta)
        {
            try
            {
                var venta = new CN_Ventas().ObtenerVenta(idVenta);
                if (venta == null) return HttpNotFound();

                // Crear ambiente (sandbox o live)
                PayPalEnvironment environment;
                if (mode == "Live")
                {
                    environment = new LiveEnvironment(clientId, clientSecret);
                }
                else
                {
                    environment = new SandboxEnvironment(clientId, clientSecret);
                }

                var client = new PayPalHttpClient(environment);
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
                                Value = venta.MontoTotal.ToString("F2"),
                                AmountBreakdown = new AmountBreakdown
                                {
                                    ItemTotal = new Money
                                    {
                                        CurrencyCode = "USD",
                                        Value = venta.MontoTotal.ToString("F2")
                                    }
                                }
                            }
                        }
                    },
                    ApplicationContext = new ApplicationContext
                    {
                        ReturnUrl = Url.Action("Completada", "PayPal", new { idVenta }, Request.Url.Scheme),
                        CancelUrl = Url.Action("Fallida", "PayPal", new { idVenta }, Request.Url.Scheme)
                    }
                });

                // ✅ CORRECCIÓN: Usar await y cast correcto
                var response = await client.Execute(request);
                var order = response.Result<Order>();

                // Buscar el link de aprobación
                var approveLink = string.Empty;
                foreach (var link in order.Links)
                {
                    if (link.Rel == "approve")
                    {
                        approveLink = link.Href;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(approveLink))
                {
                    TempData["Error"] = "No se pudo obtener el enlace de aprobación de PayPal.";
                    return RedirectToAction("Checkout", "Tienda");
                }

                return Redirect(approveLink);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al crear el pedido de pago: " + ex.Message;
                return RedirectToAction("Checkout", "Tienda");
            }
        }

        // Página de confirmación
        public ActionResult Completada(int idVenta)
        {
            var venta = new CN_Ventas().ObtenerVenta(idVenta);
            if (venta == null)
            {
                return HttpNotFound();
            }

            return View(venta);
        }

        // Página de cancelación
        public ActionResult Fallida(int idVenta)
        {
            var venta = new CN_Ventas().ObtenerVenta(idVenta);
            if (venta == null)
            {
                return HttpNotFound();
            }

            return View(venta);
        }
    }
}