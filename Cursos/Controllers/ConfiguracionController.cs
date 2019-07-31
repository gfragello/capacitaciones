using Cursos.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Cursos.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ConfiguracionController : Controller
    {
        private CursosDbContext db = new CursosDbContext();

        // GET: Configuracion
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult EditarItem()
        {
            string Index = "Direccion";
            string Seccion = "EnvioOVAL";

            var configuracionItem = db.Configuracion.Where(c => c.Index == Index && c.Seccion == Seccion).FirstOrDefault();

            if (configuracionItem == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            return View(configuracionItem);
        }
    }
}