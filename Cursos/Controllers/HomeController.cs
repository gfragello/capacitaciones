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