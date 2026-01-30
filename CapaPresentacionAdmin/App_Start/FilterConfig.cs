using System.Web;
using System.Web.Mvc;
using CapaPresentacionAdmin.Filters;

namespace CapaPresentacionAdmin
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            // Añadir autorización por sesión globalmente
            filters.Add(new SessionAuthorizeAttribute());
        }
    }
}
