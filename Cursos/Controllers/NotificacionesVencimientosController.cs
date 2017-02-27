using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Cursos.Models;
using Cursos.Helpers;
using Cursos.Models.Enums;

namespace Cursos.Controllers
{
    public class NotificacionesVencimientosController : Controller
    {
        private CursosDbContext db = new CursosDbContext();

        // GET: NotificacionesVencimientos
        public ActionResult Index()
        {
            return View(ObtenerNotificacionesVencimiento(null));
        }

        // GET: NotificacionesVencimientos/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NotificacionVencimiento notificacionVencimiento = db.NotificacionVencimientos.Find(id);
            if (notificacionVencimiento == null)
            {
                return HttpNotFound();
            }
            return View(notificacionVencimiento);
        }

        // GET: NotificacionesVencimientos/Create
        public ActionResult Create()
        {
            ViewBag.RegistroCapacitacionID = new SelectList(db.RegistroCapacitacion, "RegistroCapacitacionID", "UsuarioModificacion");
            return View();
        }

        // POST: NotificacionesVencimientos/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "NotificacionVencimientoID,MailNotificacionVencimiento,RegistroCapacitacionID")] NotificacionVencimiento notificacionVencimiento)
        {
            if (ModelState.IsValid)
            {
                db.NotificacionVencimientos.Add(notificacionVencimiento);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.RegistroCapacitacionID = new SelectList(db.RegistroCapacitacion, "RegistroCapacitacionID", "UsuarioModificacion", notificacionVencimiento.RegistroCapacitacionID);
            return View(notificacionVencimiento);
        }

        // GET: NotificacionesVencimientos/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NotificacionVencimiento notificacionVencimiento = db.NotificacionVencimientos.Find(id);
            if (notificacionVencimiento == null)
            {
                return HttpNotFound();
            }
            ViewBag.RegistroCapacitacionID = new SelectList(db.RegistroCapacitacion, "RegistroCapacitacionID", "UsuarioModificacion", notificacionVencimiento.RegistroCapacitacionID);
            return View(notificacionVencimiento);
        }

        // POST: NotificacionesVencimientos/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "NotificacionVencimientoID,MailNotificacionVencimiento,RegistroCapacitacionID")] NotificacionVencimiento notificacionVencimiento)
        {
            if (ModelState.IsValid)
            {
                db.Entry(notificacionVencimiento).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.RegistroCapacitacionID = new SelectList(db.RegistroCapacitacion, "RegistroCapacitacionID", "UsuarioModificacion", notificacionVencimiento.RegistroCapacitacionID);
            return View(notificacionVencimiento);
        }

        // GET: NotificacionesVencimientos/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NotificacionVencimiento notificacionVencimiento = db.NotificacionVencimientos.Find(id);
            if (notificacionVencimiento == null)
            {
                return HttpNotFound();
            }
            return View(notificacionVencimiento);
        }

        // POST: NotificacionesVencimientos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            NotificacionVencimiento notificacionVencimiento = db.NotificacionVencimientos.Find(id);
            db.NotificacionVencimientos.Remove(notificacionVencimiento);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

         //el parámetro jornadaIdExcluir indica que los capacitados que participaron de esas jornadas no pueden ser seleccionados
        [HttpGet]
        public ActionResult NotificacionesVencimientosAdvertencia()
        {
            /*
            var capacitados = db.Capacitados.Include(c => c.Empresa).Include(c => c.RegistrosCapacitacion);

            if (!String.IsNullOrEmpty(documento))
                capacitados = capacitados.Where(c => c.Documento.Contains(documento));

            ViewBag.JornadaIdExcluir = jornadaIdExcluir;
            */
            NotificacionesVencimientosHelper.GetInstance().ActualizarNotificacionesVencimientos();

            int totalNotificacionesPendientes = db.NotificacionVencimientos.Where(n => n.Estado == EstadoNotificacionVencimiento.NotificacionPendiente).Count();

            if (totalNotificacionesPendientes > 0)
            {
                ViewBag.TotalNotificacionesPendientes = totalNotificacionesPendientes;
                return PartialView("_NotificacionesVencimientosAdvertenciaPartial");
            }

            return Content("");
        }

        public ActionResult EnviarNotificacionesEmailEmpresa(int empresaId)
        {
            return Json(EnviarEmailsEmpresa(empresaId), JsonRequestBehavior.AllowGet);
        }

        public int EnviarEmailsEmpresa(int empresaId)
        {
            /*
            var capacitado = db.Capacitados.Where(c => c.Documento == documento).FirstOrDefault();

            if (capacitado != null)
                return capacitado.CapacitadoID;
            else
                return -1;
            */

            return -1;
         }

        private List<NotificacionVencimiento> ObtenerNotificacionesVencimiento(int? empresaId)
        {
            var notificacionVencimientos = db.NotificacionVencimientos
                                 .Where(n => n.Estado == EstadoNotificacionVencimiento.NotificacionPendiente)
                                 .OrderBy(n => n.RegistroCapacitacion.Jornada.Curso.CursoID)
                                 .OrderBy(n => n.RegistroCapacitacion.Capacitado.Empresa.NombreFantasia)
                                 .Include(n => n.RegistroCapacitacion);

            if (empresaId != null)
                notificacionVencimientos = notificacionVencimientos.Where(n => n.RegistroCapacitacion.Capacitado.EmpresaID == empresaId);

            return notificacionVencimientos.ToList();
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
