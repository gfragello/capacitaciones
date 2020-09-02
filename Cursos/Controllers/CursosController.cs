﻿using System;
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
    [Authorize(Roles = "Administrador")]
    public class CursosController : Controller
    {
        private CursosDbContext db = new CursosDbContext();

        // GET: Cursos
        public ActionResult Index()
        {
            return View(db.Cursos.ToList());
        }

        // GET: Cursos/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Curso curso = db.Cursos.Find(id);
            if (curso == null)
            {
                return HttpNotFound();
            }
            return View(curso);
        }

        // GET: Cursos/Create
        public ActionResult Create()
        {
            //se inicializa la propiedad EvaluacionConNota para que esté chequeado por defecto
            var c = new Curso { EvaluacionConNota = true };

            ViewBag.PuntoServicioId = new SelectList(db.PuntoServicio.OrderBy(p => p.Nombre).ToList(), "PuntoServicioId", "Nombre");

            return View(c);
        }

        // POST: Cursos/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CursoID,Descripcion,DescripcionEnIngles,Costo,Horas,Modulo,Vigencia,EvaluacionConNota,PuntajeMinimo,PuntajeMaximo,ColorDeFondo,RequiereAutorizacion,TieneMinimoAsistentes,MinimoAsistentes,TieneMaximoAsistentes,MaximoAsistentes,TieneCierreIncripcion,HorasCierreInscripcion,PermiteInscripcionesExternas,PermiteEnviosOVAL,PuntoServicioId")] Curso curso)
        {
            if (ModelState.IsValid)
            {
                if (!curso.PermiteEnviosOVAL)
                    curso.PuntoServicioId = null;

                db.Cursos.Add(curso);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(curso);
        }

        // GET: Cursos/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Curso curso = db.Cursos.Find(id);
            if (curso == null)
            {
                return HttpNotFound();
            }

            ViewBag.PuntoServicioId = new SelectList(db.PuntoServicio.OrderBy(p => p.Nombre).ToList(), "PuntoServicioId", "Nombre", curso.PuntoServicioId);

            return View(curso);
        }

        // POST: Cursos/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CursoID,Descripcion,DescripcionEnIngles,Costo,Horas,Modulo,Vigencia,EvaluacionConNota,PuntajeMinimo,PuntajeMaximo,ColorDeFondo,RequiereAutorizacion,TieneMinimoAsistentes,MinimoAsistentes,TieneMaximoAsistentes,MaximoAsistentes,TieneCierreIncripcion,HorasCierreInscripcion,PermiteInscripcionesExternas,PermiteEnviosOVAL,PuntoServicioId")] Curso curso)
        {
            if (ModelState.IsValid)
            {
                if (!curso.PermiteEnviosOVAL)
                    curso.PuntoServicioId = null;

                db.Entry(curso).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(curso);
        }

        // GET: Cursos/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Curso curso = db.Cursos.Find(id);
            if (curso == null)
            {
                return HttpNotFound();
            }
            return View(curso);
        }

        // POST: Cursos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Curso curso = db.Cursos.Find(id);
            db.Cursos.Remove(curso);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult ObtenerDatosPlantillaJornada(int CursoId)
        {
            var curso = db.Cursos.Where(c => c.CursoID == CursoId).FirstOrDefault();

            if (curso != null)
            {
                var datosPlantillaJornada = new
                {
                    TieneMinimoAsistentes = curso.TieneMinimoAsistentes,
                    MinimoAsistentes = curso.MinimoAsistentes,
                    TieneMaximoAsistentes = curso.TieneMaximoAsistentes,
                    MaximoAsistentes = curso.MaximoAsistentes,
                    TieneCierreIncripcion = curso.TieneCierreIncripcion,
                    HorasCierreInscripcion = curso.HorasCierreInscripcion,
                    PermiteInscripcionesExternas = curso.PermiteInscripcionesExternas,
                    PermiteEnviosOVAL = curso.PermiteEnviosOVAL
                };

                return Json(datosPlantillaJornada, JsonRequestBehavior.AllowGet);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
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
