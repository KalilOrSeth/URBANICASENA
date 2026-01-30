using System.Web;
using System.Web.Optimization;

namespace CapaPresentacionAdmin
{
    public class BundleConfig
    {
        // Para obtener más información sobre las uniones, visite https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            // jQuery
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            // Complementos (DataTables con Bootstrap 5)
            bundles.Add(new ScriptBundle("~/bundles/complementos").Include(
                "~/Scripts/fontawesome/all.min.js",
                "~/Scripts/DataTables/jquery.dataTables.js",
                "~/Scripts/DataTables/dataTables.bootstrap5.js",     // integración con Bootstrap
                "~/Scripts/DataTables/dataTables.responsive.js",
                "~/Scripts/DataTables/responsive.bootstrap5.js",     // integración responsive con Bootstrap
                "~/Scripts/loadingoverlay/loadingoverlay.min.js",
                "~/Scripts/sweetalert.min.js",
                "~/Scripts/scripts.js"
            ));

            // Bootstrap
            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.bundle.min.js"));

            // CSS global (DataTables con Bootstrap 5)
            bundles.Add(new StyleBundle("~/Content/css").Include(
                "~/Content/Site.css",
                "~/Content/DataTables/css/dataTables.bootstrap5.css",   // estilos Bootstrap
                "~/Content/DataTables/css/responsive.bootstrap5.css",   // estilos responsive Bootstrap
                "~/Content/sweetalert.css"
            ));
        }
    }
}
