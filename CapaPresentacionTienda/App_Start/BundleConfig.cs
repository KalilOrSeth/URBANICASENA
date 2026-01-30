using System.Web;
using System.Web.Optimization;

namespace CapaPresentacionTienda
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            // jQuery sí puede ir en bundle
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            // No incluimos bootstrap.bundle.min.js en el bundle para evitar errores de minificación con sintaxis moderna
            // Si necesitas agruparlo, usa NUglify o carga desde CDN (recomendado para Bootstrap 5)
            // bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
            //     "~/Scripts/bootstrap.bundle.min.js"));

            // CSS sí puede ir en bundle
            bundles.Add(new StyleBundle("~/Content/css").Include(
                        "~/Content/bootstrap.css",
                        "~/Content/Site.css"));
        }
    }
}
