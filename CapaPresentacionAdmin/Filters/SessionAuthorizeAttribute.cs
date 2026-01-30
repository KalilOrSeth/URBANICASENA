using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CapaPresentacionAdmin.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SessionAuthorizeAttribute : AuthorizeAttribute
    {
        public string Roles { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Allow access to the Acceso controller (login, recovery) and static resources
            var routeData = httpContext.Request.RequestContext?.RouteData;
            var controller = routeData?.Values["controller"]?.ToString() ?? string.Empty;
            var action = routeData?.Values["action"]?.ToString() ?? string.Empty;

            if (string.Equals(controller, "Acceso", StringComparison.OrdinalIgnoreCase))
                return true;

            var path = httpContext.Request.Path ?? string.Empty;
            if (path.Contains("/Content/") || path.Contains("/Scripts/") || path.Contains("/images/") || path.Contains("/img/"))
                return true;

            // If no session role, not authorized
            if (httpContext.Session == null || httpContext.Session["Role"] == null)
                return false;

            string rolSesion = httpContext.Session["Role"].ToString();

            // If Roles is empty, any authenticated session is valid
            if (string.IsNullOrEmpty(Roles))
                return true;

            // Validate role (case-insensitive)
            var allowed = Roles.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(r => r.Trim())
                               .Any(r => string.Equals(r, rolSesion, StringComparison.OrdinalIgnoreCase));

            return allowed;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // If request is for the admin login, show it (avoid redirect loop)
            var controller = filterContext.RouteData.Values["controller"]?.ToString() ?? string.Empty;
            if (string.Equals(controller, "Acceso", StringComparison.OrdinalIgnoreCase))
            {
                filterContext.Result = new ViewResult { ViewName = "Index" };
                return;
            }

            // Redirect to admin login
            filterContext.Result = new RedirectResult("~/Acceso/Index");
        }
    }
}
