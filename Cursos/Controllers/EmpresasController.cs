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
using OfficeOpenXml;
using System.Drawing;
using System.IO;

namespace Cursos.Controllers
{
    [Authorize(Roles = "Administrador,AdministradorExterno")]
    public class EmpresasController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Empresas
        public ActionResult Index(string currentFiltroEmpresa, string filtroEmpresa, int? FiltroEmpresaID, int? FiltroRegimenPagoID, int? page, bool? exportarExcel)
        {
            if (filtroEmpresa != null)
                page = 1;
            else
                filtroEmpresa = currentFiltroEmpresa;

            ViewBag.CurrentFiltroEmpresa = filtroEmpresa;
            ViewBag.FiltroRegimenPagoID = FiltroRegimenPagoID;
            ViewBag.RegimenesPago = new SelectList(db.RegimenesPago, "RegimenPagoID", "Nombre", FiltroRegimenPagoID);

            var empresas = db.Empresas.Include(e => e.RegimenPago).AsQueryable();

            if (FiltroEmpresaID.HasValue && FiltroEmpresaID.Value > 0)
                empresas = empresas.Where(e => e.EmpresaID == FiltroEmpresaID.Value);
            else if(!String.IsNullOrEmpty(filtroEmpresa))
            {
                empresas = empresas.Where(e =>
                    (e.NombreFantasia != null && e.NombreFantasia.Contains(filtroEmpresa)) ||
                    (e.RazonSocial != null && e.RazonSocial.Contains(filtroEmpresa)) ||
                    (e.RUT != null && e.RUT.Contains(filtroEmpresa))
                );
            }
            
            if (FiltroRegimenPagoID.HasValue && FiltroRegimenPagoID.Value > 0)
                empresas = empresas.Where(e => e.RegimenPagoID == FiltroRegimenPagoID.Value);

            int pageSize = 10;
            int pageNumber = (page ?? 1);

            empresas = empresas.OrderBy(o => o.NombreFantasia);

            ViewBag.TotalEmpresas = empresas.Count();

            bool exportar = exportarExcel != null ? (bool)exportarExcel : false;

            if (!exportar)
                return View(empresas.ToPagedList(pageNumber, pageSize));
            else
                return ExportDataExcel(empresas.ToList());
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

            ViewBag.Cursos = db.Cursos.Where(c => c.MostrarEnIndexCapacitado).OrderBy(c => c.Descripcion).ToList();

            return View(empresa);
        }

        // GET: Empresas/Create
        public ActionResult Create()
        {
            ViewBag.DepartamentoID = new SelectList(db.Departamentos, "DepartamentoID", "Nombre");
            ViewBag.RegimenPagoID = new SelectList(db.RegimenesPago, "RegimenPagoID", "Nombre", 1); // 1 = Régimen Estándar por defecto
            
            // Asegurarnos que el modelo tenga un valor por defecto
            var empresa = new Empresa { RegimenPagoID = 2 };
            
            return View(empresa);
        }

        // POST: Empresas/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "EmpresaID,NombreFantasia,Domicilio,RazonSocial,RUT,DepartamentoID,Localidad,CodigoPostal,Email,EmailFacturacion,Telefono,RegimenPagoID")] Empresa empresa)
        {
            if (ModelState.IsValid)
            {
                empresa.SetearAtributosControl();

                db.Empresas.Add(empresa);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.DepartamentoID = new SelectList(db.Departamentos, "DepartamentoID", "Nombre");
            ViewBag.RegimenPagoID = new SelectList(db.RegimenesPago, "RegimenPagoID", "Nombre");

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
                ViewBag.RegimenPagoID = new SelectList(db.RegimenesPago, "RegimenPagoID", "Nombre", empresa.RegimenPagoID);
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
        public ActionResult Edit([Bind(Include = "EmpresaID,NombreFantasia,Domicilio,RazonSocial,RUT,DepartamentoID,Localidad,CodigoPostal,Email,EmailFacturacion,Telefono,RegimenPagoID")] Empresa empresa)
        {
            if (ModelState.IsValid)
            {
                empresa.SetearAtributosControl();

                db.Entry(empresa).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.DepartamentoID = new SelectList(db.Departamentos, "DepartamentoID", "Nombre");
            ViewBag.RegimenPagoID = new SelectList(db.RegimenesPago, "RegimenPagoID", "Nombre", empresa.RegimenPagoID);

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

        private ActionResult ExportDataExcel(List<Empresa> empresas)
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Empresas");

                const int rowInicial = 1;
                int i = rowInicial + 1;

                ws.Cells[rowInicial, 1].Value = "Nombre de fantasía";
                ws.Cells[rowInicial, 2].Value = "Domicilio";
                ws.Cells[rowInicial, 3].Value = "Razón Social";
                ws.Cells[rowInicial, 4].Value = "RUT";
                ws.Cells[rowInicial, 5].Value = "Departamento";
                ws.Cells[rowInicial, 6].Value = "Localidad";
                ws.Cells[rowInicial, 7].Value = "Código Postal";
                ws.Cells[rowInicial, 8].Value = "Email";
                ws.Cells[rowInicial, 9].Value = "Email Facturación";

                ws.Cells[rowInicial, 1, rowInicial, 9].Style.Font.Bold = true;

                var bgColor = Color.White;

                foreach (var e in empresas)
                {
                    ws.Cells[i, 1].Value = e.NombreFantasia;
                    ws.Cells[i, 2].Value = e.Domicilio;
                    ws.Cells[i, 3].Value = e.RazonSocial;
                    ws.Cells[i, 4].Value = e.RUT;
                    ws.Cells[i, 5].Value = e.Departamento.Nombre;
                    ws.Cells[i, 6].Value = e.Localidad;
                    ws.Cells[i, 7].Value = e.CodigoPostal;
                    ws.Cells[i, 8].Value = e.Email;
                    ws.Cells[i, 9].Value = e.EmailFacturacion;

                    //se seleccionan las columnas con datos del capacitado para setear el background color.
                    if (bgColor != Color.White)
                    {
                        ws.Cells[i - 1, 1, i - 1, 9].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[i - 1, 1, i - 1, 9].Style.Fill.BackgroundColor.SetColor(bgColor);
                    }

                    //se pone un borde alrededor del renglón
                    ws.Cells[i - 1, 1, i - 1, 9].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    bgColor = bgColor == Color.White ? Color.WhiteSmoke : Color.White;

                    i++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = "empresas.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }

        [HttpGet]
        public JsonResult AutocompleteEmpresa(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            var sugeridas = db.Empresas
                .Where(e =>
                    (e.NombreFantasia != null && e.NombreFantasia.Contains(term)) ||
                    (e.RazonSocial != null && e.RazonSocial.Contains(term)) ||
                    (e.RUT != null && e.RUT.Contains(term))
                )
                .OrderBy(e => e.NombreFantasia)
                .Take(10)
                .ToList() // Salimos de LINQ-to-Entities para usar lógica C#
                .Select(e => new
                {
                    id = e.EmpresaID,
                    label = e.NombreFantasia
                                + (!string.IsNullOrEmpty(e.RazonSocial) ? " (" + e.RazonSocial + ")" : "")
                                + (!string.IsNullOrEmpty(e.RUT) ? " - " + e.RUT : ""),
                    value = (string.IsNullOrWhiteSpace(e.RUT) || string.IsNullOrWhiteSpace(e.RazonSocial))
                                ? e.NombreFantasia
                                : string.Format("{0} / {1}", e.RazonSocial, e.RUT)
                })
                .ToList();

            return Json(sugeridas, JsonRequestBehavior.AllowGet);
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
