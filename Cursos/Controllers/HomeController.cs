using Cursos.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Cursos.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            string url = Request.Url.Host;

            if (Request.Url.Host == "jornadas.csl.uy" || (System.Web.HttpContext.Current.User.IsInRole("InscripcionesExternas")) && !System.Web.HttpContext.Current.User.IsInRole("ConsultaEmpresa"))
                //si se entra por la URL jornadas.csl.uy se navega a las página de Jornadas Disponibles
                return RedirectToAction("Disponibles", "Jornadas");
            else
                //el punto de inicio de la aplicación será el index de capacitados
                return RedirectToAction("Index", "Capacitados");

            //return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}