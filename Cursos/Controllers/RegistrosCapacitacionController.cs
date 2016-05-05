using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Cursos.Models;

namespace Cursos.Controllers
{
    [Authorize(Users = "guillefra@gmail.com,alejandro.lacruz@gmail.com")]
    public class RegistrosCapacitacionController : Controller
    {
        private CursosDbContext db = new CursosDbContext();

        // GET: RegistrosCapacitacion
        public ActionResult Index(int? cursoID, DateTime? fechaInicio, DateTime? fechaFin, int? notaDesde, int? notaHasta)
        {
            var registrosCapacitacion = db.RegistroCapacitacion.Include(r => r.Capacitado).Include(r => r.Jornada);

            if (cursoID != null && cursoID != -1)
                registrosCapacitacion = registrosCapacitacion.Where(r => r.Jornada.Curso.CursoID == cursoID);

            if (fechaInicio != null)
            {
                ViewBag.FechaInicio = fechaInicio;
                registrosCapacitacion = registrosCapacitacion.Where(r => r.Jornada.Fecha >= fechaInicio.Value);
            }

            if (fechaFin != null)
            {
                fechaFin = new DateTime(fechaFin.Value.Year, fechaFin.Value.Month, fechaFin.Value.Day, 23, 59, 59);
                ViewBag.FechaFin = fechaFin;
                registrosCapacitacion = registrosCapacitacion.Where(r => r.Jornada.Fecha <= fechaFin.Value);
            }

            if (notaDesde != null)
            {
                ViewBag.NotaDesde = notaDesde;
                registrosCapacitacion = registrosCapacitacion.Where(r => r.Nota >= notaDesde);
            }

            if (notaHasta != null)
            {
                ViewBag.NotaHasta = notaHasta;
                registrosCapacitacion = registrosCapacitacion.Where(r => r.Nota <= notaHasta);
            }

            List<Curso> cursosDD = db.Cursos.OrderBy(c => c.Descripcion).ToList();
            cursosDD.Insert(0, new Curso { CursoID = -1, Descripcion = "Todos" });
            ViewBag.CursoID = new SelectList(cursosDD, "CursoID", "Descripcion", cursoID);

            registrosCapacitacion = registrosCapacitacion.OrderByDescending(r => r.Jornada.Fecha);

            return View(registrosCapacitacion.ToList());
        }

        // GET: RegistrosCapacitacion/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RegistroCapacitacion registroCapacitacion = db.RegistroCapacitacion.Find(id);
            if (registroCapacitacion == null)
            {
                return HttpNotFound();
            }
            return View(registroCapacitacion);
        }

        // GET: RegistrosCapacitacion/Create
        public ActionResult Create()
        {
            //solamente se podrán crear registros de capacitación usando el procedimiento CreateWithCapacitado
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        public ActionResult CreateWithCapacitado(int? id)
        {
            ViewBag.JornadaID = new SelectList(db.Jornada.OrderByDescending(j => j.Fecha).ThenByDescending(j => j.Hora).ToList(), "JornadaID", "JornadaIdentificacionCompleta");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else
            {
                var capacitado = db.Capacitados.Find(id);

                if (capacitado != null)
                {
                    ViewBag.Capacitado = capacitado;
                    ViewBag.CapacitadoID = capacitado.CapacitadoID;
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }

            return View("Create");
        }

        // POST: RegistrosCapacitacion/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateWithCapacitado([Bind(Include = "RegistroCapacitacionID,Aprobado,Nota,NotaPrevia,JornadaID,CapacitadoID,FechaVencimiento")] RegistroCapacitacion registroCapacitacion)
        {
            if (ModelState.IsValid)
            {
                db.RegistroCapacitacion.Add(registroCapacitacion);
                db.SaveChanges();
                //return RedirectToAction("Index");
                return RedirectToAction("Details", "Capacitados", new { id = registroCapacitacion.CapacitadoID });
            }

            var capacitado = db.Capacitados.Find(registroCapacitacion.CapacitadoID);

            if (capacitado != null)
            {
                ViewBag.Capacitado = capacitado;
                ViewBag.CapacitadoID = capacitado.CapacitadoID;
            }
            else
            {
                return new HttpNotFoundResult();
            }

            ViewBag.JornadaID = new SelectList(db.Jornada.ToList(), "JornadaID", "JornadaIdentificacionCompleta", registroCapacitacion.JornadaID);
            return View("Create");
        }

        // POST: RegistrosCapacitacion/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RegistroCapacitacionID,Aprobado,Nota,NotaPrevia,JornadaID,CapacitadoID,FechaVencimiento")] RegistroCapacitacion registroCapacitacion)
        {
            if (ModelState.IsValid)
            {
                db.RegistroCapacitacion.Add(registroCapacitacion);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CapacitadoID = new SelectList(db.Capacitados, "CapacitadoID", "Nombre", registroCapacitacion.CapacitadoID);
            ViewBag.JornadaID = new SelectList(db.Jornada, "JornadaID", "JornadaID", registroCapacitacion.JornadaID);
            return View(registroCapacitacion);
        }

        // GET: RegistrosCapacitacion/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RegistroCapacitacion registroCapacitacion = db.RegistroCapacitacion.Find(id);
            if (registroCapacitacion == null)
            {
                return HttpNotFound();
            }
            ViewBag.CapacitadoID = new SelectList(db.Capacitados, "CapacitadoID", "Nombre", registroCapacitacion.CapacitadoID);
            ViewBag.JornadaID = new SelectList(db.Jornada, "JornadaID", "JornadaID", registroCapacitacion.JornadaID);
            return View(registroCapacitacion);
        }

        // POST: RegistrosCapacitacion/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RegistroCapacitacionID,Aprobado,Nota,JornadaID,CapacitadoID,FechaVencimiento")] RegistroCapacitacion registroCapacitacion)
        {
            if (ModelState.IsValid)
            {
                db.Entry(registroCapacitacion).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CapacitadoID = new SelectList(db.Capacitados, "CapacitadoID", "Nombre", registroCapacitacion.CapacitadoID);
            ViewBag.JornadaID = new SelectList(db.Jornada, "JornadaID", "JornadaID", registroCapacitacion.JornadaID);
            return View(registroCapacitacion);
        }

        // GET: RegistrosCapacitacion/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RegistroCapacitacion registroCapacitacion = db.RegistroCapacitacion.Find(id);
            if (registroCapacitacion == null)
            {
                return HttpNotFound();
            }
            return View(registroCapacitacion);
        }

        // POST: RegistrosCapacitacion/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            RegistroCapacitacion registroCapacitacion = db.RegistroCapacitacion.Find(id);
            int capacitadoID = registroCapacitacion.CapacitadoID;
            db.RegistroCapacitacion.Remove(registroCapacitacion);
            db.SaveChanges();
            return RedirectToAction("Details", "Capacitados", new { id = capacitadoID });
        }

        public ActionResult ObtenerFechaVencimiento(int JornadaId)
        {
            return Json(FechaVencimientoString(JornadaId), JsonRequestBehavior.AllowGet);
        }

        private string FechaVencimientoString(int JornadaId)
        {
            DateTime FechaVencimiento = CalcularFechaVencimiento(JornadaId);
            return FechaVencimiento.Day.ToString().PadLeft(2, '0') + "/" + FechaVencimiento.Month.ToString().PadLeft(2, '0') + "/" + FechaVencimiento.Year.ToString();
        }

        private DateTime CalcularFechaVencimiento(int JornadaId)
        {
            var j = db.Jornada.Find(JornadaId);
            return new DateTime(j.Fecha.Year + j.Curso.Vigencia, j.Fecha.Month, j.Fecha.Day);
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
