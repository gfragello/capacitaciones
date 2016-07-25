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
    [Authorize(Roles = "Administrador,AdministradorExterno")]
    public class InstructoresController : Controller
    {
        private CursosDbContext db = new CursosDbContext();

        // GET: Instructores
        public ActionResult Index(bool? soloActivos)
        {
            List<Instructor> instructores = null;

            bool activos = soloActivos != null ? (bool)soloActivos : false;

            if (!activos)
                instructores = db.Instructores.ToList();
            else
                instructores = db.Instructores.Where(i => i.Activo == true).ToList();

            return View(instructores);
        }

        // GET: Instructores/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Instructor instructor = db.Instructores.Find(id);
            if (instructor == null)
            {
                return HttpNotFound();
            }
            return View(instructor);
        }

        // GET: Instructores/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Instructores/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "InstructorID,Nombre,Apellido,Documento,FechaNacimiento,Domicilio,Telefono,Activo")] Instructor instructor)
        {
            if (ModelState.IsValid)
            {
                instructor.SetearAtributosControl();

                db.Instructores.Add(instructor);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(instructor);
        }

        // GET: Instructores/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Instructor instructor = db.Instructores.Find(id);
            if (instructor == null)
            {
                return HttpNotFound();
            }

            if (instructor.PuedeModificarse())
                return View(instructor);
            else
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
        }

        // POST: Instructores/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "InstructorID,Nombre,Apellido,Documento,FechaNacimiento,Domicilio,Telefono,Activo")] Instructor instructor)
        {
            if (ModelState.IsValid)
            {
                instructor.SetearAtributosControl();

                db.Entry(instructor).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(instructor);
        }

        // GET: Instructores/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Instructor instructor = db.Instructores.Find(id);
            if (instructor == null)
            {
                return HttpNotFound();
            }

            if (instructor.PuedeModificarse())
                return View(instructor);
            else
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
        }

        // POST: Instructores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Instructor instructor = db.Instructores.Find(id);
                db.Instructores.Remove(instructor);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
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
