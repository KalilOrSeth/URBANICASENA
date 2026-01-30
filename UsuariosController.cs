using System.Web.Mvc;
using CapaNegocio;
using CapaEntidad;
using CapaPresentacionAdmin.Filters;

namespace CapaPresentacionAdmin.Controllers
{
    [SessionAuthorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {
        private readonly CN_Usuarios _cnUsuarios = new CN_Usuarios();

        public ActionResult Index()
        {
            var lista = _cnUsuarios.Listar();
            return View(lista);
        }

        public ActionResult Details(int id)
        {
            var usuario = _cnUsuarios.Listar().Find(u => u.IdUsuario == id);
            if (usuario == null) return HttpNotFound();
            return View(usuario);
        }
    }
}