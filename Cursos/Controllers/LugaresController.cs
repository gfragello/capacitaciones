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
    public class LugaresController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Lugares
        public ActionResult Index()
        {
            return View(db.Lugares.ToList());
        }

        // GET: Lugares/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Lugar lugar = db.Lugares.Find(id);
            if (lugar == null)
            {
                return HttpNotFound();
            }
            return View(lugar);
        }

        // GET: Lugares/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Lugares/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "LugarID,NombreLugar,AbrevLugar,DireccionHabitual")] Lugar lugar)
        {
            if (ModelState.IsValid)
            {
                db.Lugares.Add(lugar);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(lugar);
        }

        // GET: Lugares/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Lugar lugar = db.Lugares.Find(id);
            if (lugar == null)
            {
                return HttpNotFound();
            }
            return View(lugar);
        }

        // POST: Lugares/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "LugarID,NombreLugar,AbrevLugar,DireccionHabitual")] Lugar lugar)
        {
            if (ModelState.IsValid)
            {
                db.Entry(lugar).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(lugar);
        }

        // GET: Lugares/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Lugar lugar = db.Lugares.Find(id);
            if (lugar == null)
            {
                return HttpNotFound();
            }
            return View(lugar);
        }

        // POST: Lugares/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Lugar lugar = db.Lugares.Find(id);
                db.Lugares.Remove(lugar);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        public ActionResult ObtenerDireccionHabitual(int LugarId)
        {
            string direccionHabitual = string.Empty;
            //string enabled = Model.Capacitados.Count > 0 ? "disabled='disabled'" : String.Empty;

            var lugar = db.Lugares.Where(l => l.LugarID == LugarId).FirstOrDefault();

            if (lugar != null)
                direccionHabitual = lugar.DireccionHabitual != null ? lugar.DireccionHabitual : string.Empty;

            return Json(direccionHabitual, JsonRequestBehavior.AllowGet);
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
