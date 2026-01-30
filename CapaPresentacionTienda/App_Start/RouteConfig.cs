using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace CapaPresentacionTienda
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Ruta especial para que /Acceso funcione sin /Index
            routes.MapRoute(
                name: "AccesoDefault",
                url: "Acceso",
                defaults: new { controller = "Acceso", action = "Index" }
            );

            // Ruta por defecto
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Tienda", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}

