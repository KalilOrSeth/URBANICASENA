using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CapaPresentacionTienda.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SessionAuthorizeAttribute : AuthorizeAttribute
    {
        public string Roles { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Allow access to Acceso controller (login, registration, password recovery)
            var routeData = httpContext.Request.RequestContext?.RouteData;
            var controller = routeData?.Values["controller"]?.ToString() ?? string.Empty;

            if (string.Equals(controller, "Acceso", StringComparison.OrdinalIgnoreCase))
                return true;

            // Allow static resources
            var path = httpContext.Request.Path ?? string.Empty;
            if (path.Contains("/Content/") || path.Contains("/Scripts/") || path.Contains("/images/") || path.Contains("/img/"))
                return true;

            // Check if user has session
            if (httpContext.Session == null || httpContext.Session["Role"] == null)
                return false;

            string rolSesion = httpContext.Session["Role"].ToString();

            // If no specific roles required, any authenticated session is valid
            if (string.IsNullOrEmpty(Roles))
                return true;

            // Validate role (case-insensitive, comma-separated)
            var allowed = Roles.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(r => r.Trim())
                               .Any(r => string.Equals(r, rolSesion, StringComparison.OrdinalIgnoreCase));

            return allowed;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // If unauthorized, redirect to login
            filterContext.Result = new RedirectResult("~/Acceso/Index");
        }
    }
}