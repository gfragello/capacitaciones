using System.Linq;
using System.Web.Mvc;
using Cursos.Models;
using Cursos.Models.ViewModels;

namespace Cursos.Controllers
{
    // Ajusta el [Authorize] según los roles que necesites
    [Authorize(Roles = "Administrador")]
    public class MensajesUsuariosController : Controller
    {
        private CursosDbContext db = new CursosDbContext();

        // GET: MensajesUsuarios/Edit
        public ActionResult Edit()
        {
            // Se obtienen los mensajes correspondientes a los identificadores
            var mensaje1 = db.MensajesUsuarios.FirstOrDefault(m => m.IdentificadorInterno == "DisponiblesMensaje1");
            var mensaje2 = db.MensajesUsuarios.FirstOrDefault(m => m.IdentificadorInterno == "DisponiblesMensaje2");

            // Si alguno no existe, se crea un objeto con valores por defecto
            if (mensaje1 == null)
            {
                mensaje1 = new MensajeUsuario { IdentificadorInterno = "DisponiblesMensaje1", Cuerpo = "" };
            }
            if (mensaje2 == null)
            {
                mensaje2 = new MensajeUsuario { IdentificadorInterno = "DisponiblesMensaje2", Cuerpo = "" };
            }

            var viewModel = new EditMensajesUsuariosViewModel
            {
                Mensaje1 = mensaje1,
                Mensaje2 = mensaje2
            };

            return View(viewModel);
        }

        // POST: MensajesUsuarios/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EditMensajesUsuariosViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Se buscan los mensajes en la base de datos para actualizarlos
                var mensaje1Db = db.MensajesUsuarios.FirstOrDefault(m => m.IdentificadorInterno == "DisponiblesMensaje1");
                var mensaje2Db = db.MensajesUsuarios.FirstOrDefault(m => m.IdentificadorInterno == "DisponiblesMensaje2");

                if (mensaje1Db != null)
                {
                    mensaje1Db.Cuerpo = viewModel.Mensaje1.Cuerpo;
                }
                else
                {
                    db.MensajesUsuarios.Add(new MensajeUsuario
                    {
                        IdentificadorInterno = "DisponiblesMensaje1",
                        Cuerpo = viewModel.Mensaje1.Cuerpo
                    });
                }

                if (mensaje2Db != null)
                {
                    mensaje2Db.Cuerpo = viewModel.Mensaje2.Cuerpo;
                }
                else
                {
                    db.MensajesUsuarios.Add(new MensajeUsuario
                    {
                        IdentificadorInterno = "DisponiblesMensaje2",
                        Cuerpo = viewModel.Mensaje2.Cuerpo
                    });
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "Los mensajes han sido actualizados correctamente.";
                return RedirectToAction("Edit");
            }
            return View(viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
