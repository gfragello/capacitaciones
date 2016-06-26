using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Cursos.Models;
using PagedList;

namespace Cursos.Controllers
{
    [Authorize(Roles = "Administrador,AdministradorExterno")]
    public class EmpresasController : Controller
    {
        private CursosDbContext db = new CursosDbContext();

        // GET: Empresas
        public ActionResult Index(string currentNombreFantasia, string nombreFantasia,
                                  int? page)
        {
            if (nombreFantasia != null) //si el parámetro vino con algún valor es porque se presionó buscar y se resetea la página a 1
                page = 1;
            else
                nombreFantasia = currentNombreFantasia;

            ViewBag.CurrentNombreFantasia = nombreFantasia;

            var empresas = db.Empresas.AsQueryable();

            if (!String.IsNullOrEmpty(nombreFantasia))
                empresas = empresas.Where(e => e.NombreFantasia.Contains(nombreFantasia));

            int pageSize = 10;
            int pageNumber = (page ?? 1);

            empresas = empresas.OrderBy(o => o.NombreFantasia);

            ViewBag.TotalEmpresas = empresas.Count();

            return View(empresas.ToPagedList(pageNumber, pageSize));
        }

        // GET: Empresas/Details/5
        public ActionResult Details(int? id, int? page)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Empresa empresa = db.Empresas.Find(id);
            if (empresa == null)
            {
                return HttpNotFound();
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);

            var capacitados = db.Capacitados.Where(c => c.EmpresaID == id).Include(c => c.RegistrosCapacitacion);
            ViewBag.Capacitados = capacitados.OrderBy(c => c.Apellido).ToPagedList(pageNumber, pageSize);

            ViewBag.Cursos = db.Cursos.OrderBy(c => c.Descripcion).ToList();

            return View(empresa);
        }

        // GET: Empresas/Create
        public ActionResult Create()
        {
            ViewBag.DepartamentoID = new SelectList(db.Departamentos, "DepartamentoID", "Nombre");

            return View();
        }

        // POST: Empresas/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "EmpresaID,NombreFantasia,Domicilio,RazonSocial,RUT,DepartamentoID,Localidad,CodigoPostal,Email")] Empresa empresa)
        {
            if (ModelState.IsValid)
            {
                empresa.SetearAtributosControl();

                db.Empresas.Add(empresa);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.DepartamentoID = new SelectList(db.Departamentos, "DepartamentoID", "Nombre");

            return View(empresa);
        }

        // GET: Empresas/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Empresa empresa = db.Empresas.Find(id);
            if (empresa == null)
            {
                return HttpNotFound();
            }

            if (empresa.PuedeModificarse())
            {
                ViewBag.DepartamentoID = new SelectList(db.Departamentos, "DepartamentoID", "Nombre");
                return View(empresa);
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }
        }

        // POST: Empresas/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "EmpresaID,NombreFantasia,Domicilio,RazonSocial,RUT,DepartamentoID,Localidad,CodigoPostal,Email")] Empresa empresa)
        {
            if (ModelState.IsValid)
            {
                empresa.SetearAtributosControl();

                db.Entry(empresa).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.DepartamentoID = new SelectList(db.Departamentos, "DepartamentoID", "Nombre");

            return View(empresa);
        }

        // GET: Empresas/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Empresa empresa = db.Empresas.Find(id);
            if (empresa == null)
            {
                return HttpNotFound();
            }

            if (empresa.PuedeModificarse())
                return View(empresa);
            else
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
        }

        // POST: Empresas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Empresa empresa = db.Empresas.Find(id);
            db.Empresas.Remove(empresa);
            db.SaveChanges();
            return RedirectToAction("Index");
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
