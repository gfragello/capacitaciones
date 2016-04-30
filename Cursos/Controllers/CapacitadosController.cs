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
using System.IO;
using System.Drawing;

namespace Cursos.Controllers
{
    public class CapacitadosController : Controller
    {
        private CursosDbContext db = new CursosDbContext();

        // GET: Capacitados
        public ActionResult Index(string documento, 
                                  string currentNombre, string nombre,
                                  string currentApellido, string apellido, 
                                  int? currentEmpresaID, int? EmpresaID, 
                                  int? currentCursoID, int? CursoID, 
                                  int? page, bool? exportarExcel)
        {
            if (nombre != null) //si el parámetro vino con algún valor es porque se presionó buscar y se resetea la página a 1
                page = 1;
            else
                nombre = currentNombre;

            ViewBag.CurrentNombre = nombre;

            if (apellido != null)
                page = 1;
            else
                apellido = currentApellido;

            ViewBag.CurrentApellido = apellido;

            if (EmpresaID != null)
                page = 1;
            else
            { 
                if (currentEmpresaID == null)
                    currentEmpresaID = -1;

                EmpresaID = currentEmpresaID;
            }

            ViewBag.CurrentEmpresaID = EmpresaID;

            if (CursoID != null)
                page = 1;
            else
            {
                if (currentCursoID == null)
                    currentCursoID = -1;

                CursoID = currentCursoID;
            }

            ViewBag.CurrentCursoID = CursoID;

            List<Empresa> empresasDD = db.Empresas.OrderBy(e => e.NombreFantasia).ToList();
            empresasDD.Insert(0, new Empresa { EmpresaID = -1, NombreFantasia = "Todas" });
            ViewBag.EmpresaID = new SelectList(empresasDD, "EmpresaID", "NombreFantasia", EmpresaID);

            List<Curso> cursosDD = db.Cursos.OrderBy(c => c.Descripcion).ToList();
            cursosDD.Insert(0, new Curso { CursoID = -1, Descripcion = "Todos" });
            ViewBag.CursoID = new SelectList(cursosDD, "CursoID", "Descripcion", CursoID);

            var capacitados = db.Capacitados.Include(c => c.Empresa).Include(c => c.RegistrosCapacitacion);

            if (!String.IsNullOrEmpty(documento))
                capacitados = capacitados.Where(c => c.Documento.Contains(documento));

            if (!String.IsNullOrEmpty(nombre))
                capacitados = capacitados.Where(c => c.Nombre.Contains(nombre));

            if (!String.IsNullOrEmpty(apellido))
                capacitados = capacitados.Where(c => c.Apellido.Contains(apellido));

            if (EmpresaID != -1)
                capacitados = capacitados.Where(c => c.EmpresaID == (int)EmpresaID);

            if (CursoID != -1)
                capacitados = capacitados.Where(c => c.RegistrosCapacitacion.Any(r => r.Jornada.CursoId == (int)CursoID));

            capacitados = capacitados.OrderBy(c => c.Apellido).ThenBy(c => c.Nombre);

            int pageSize = 10;
            int pageNumber = (page ?? 1);

            ViewBag.Cursos = db.Cursos.OrderBy(c => c.Descripcion).ToList();

            bool exportar = exportarExcel != null ? (bool)exportarExcel : false;

            if (!exportar)
                return View(capacitados.ToPagedList(pageNumber, pageSize));
            else
                return ExportDataExcel(capacitados.ToList(), CursoID);
        }

        // GET: Capacitados/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var capacitados = db.Capacitados.Include(c => c.RegistrosCapacitacion);
            var capacitado = capacitados.Where(c => c.CapacitadoID == (int)id).First();

            if (capacitado == null)
            {
                return HttpNotFound();
            }
            return View(capacitado);
        }

        // GET: Capacitados/Create
        [Authorize(Users = "guillefra@gmail.com,alejandro.lacruz@gmail.com")]
        public ActionResult Create()
        {
            ViewBag.EmpresaID = new SelectList(db.Empresas.OrderBy(e => e.NombreFantasia).ToList(), "EmpresaID", "NombreFantasia");
            ViewBag.TipoDocumentoID = new SelectList(db.TiposDocumento.ToList(), "TipoDocumentoID", "Descripcion");
            return View();
        }

        // POST: Capacitados/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CapacitadoID,Nombre,Apellido,Documento,Fecha,EmpresaID,TipoDocumentoID")] Capacitado capacitado)
        {
            if (ModelState.IsValid)
            {
                db.Capacitados.Add(capacitado);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.TipoDocumentoID = new SelectList(db.TiposDocumento.ToList(), "TipoDocumentoID", "Descripcion", capacitado.TipoDocumentoID);
            ViewBag.EmpresaID = new SelectList(db.Empresas.OrderBy(e => e.NombreFantasia).ToList(), "EmpresaID", "NombreFantasia", capacitado.EmpresaID);

            return View(capacitado);
        }

        // GET: Capacitados/Edit/5
        [Authorize(Users = "guillefra@gmail.com,alejandro.lacruz@gmail.com")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Capacitado capacitado = db.Capacitados.Find(id);
            if (capacitado == null)
            {
                return HttpNotFound();
            }
            ViewBag.EmpresaID = new SelectList(db.Empresas.OrderBy(e => e.NombreFantasia).ToList(), "EmpresaID", "NombreFantasia", capacitado.EmpresaID);
            ViewBag.TipoDocumentoID = new SelectList(db.TiposDocumento.ToList(), "TipoDocumentoID", "Descripcion", capacitado.TipoDocumentoID);
            return View(capacitado);
        }

        // POST: Capacitados/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CapacitadoID,Nombre,Apellido,Documento,Fecha,EmpresaID,TipoDocumentoID")] Capacitado capacitado)
        {
            if (ModelState.IsValid)
            {
                db.Entry(capacitado).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.EmpresaID = new SelectList(db.Empresas.OrderBy(e => e.NombreFantasia).ToList(), "EmpresaID", "NombreFantasia", capacitado.EmpresaID);
            ViewBag.TipoDocumentoID = new SelectList(db.TiposDocumento.ToList(), "TipoDocumentoID", "Descripcion", capacitado.TipoDocumentoID);
            return View(capacitado);
        }

        // GET: Capacitados/Delete/5
        [Authorize(Users = "guillefra@gmail.com,alejandro.lacruz@gmail.com")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Capacitado capacitado = db.Capacitados.Find(id);
            if (capacitado == null)
            {
                return HttpNotFound();
            }
            return View(capacitado);
        }

        // POST: Capacitados/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Capacitado capacitado = db.Capacitados.Find(id);
            db.Capacitados.Remove(capacitado);
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

        private ActionResult ExportDataExcel(List<Capacitado> capacitados, int? CursoID)
        {

            using (ExcelPackage package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Capacitados");

                const int rowInicial = 1;
                int i = rowInicial + 1;
                
                ws.Cells[rowInicial, 1].Value = "Apellido";
                ws.Cells[rowInicial, 2].Value = "Nombre";
                ws.Cells[rowInicial, 3].Value = "Documento";
                ws.Cells[rowInicial, 4].Value = "Empresa";
                ws.Cells[rowInicial, 5].Value = "Últimos cursos";
                ws.Cells[rowInicial, 6].Value = "Instructor";

                ws.Cells[rowInicial, 1, rowInicial, 6].Style.Font.Bold = true;

                var bgColor = Color.White;

                foreach (var c in capacitados)
                {
                    ws.Cells[i, 1].Value = c.Apellido;
                    ws.Cells[i, 2].Value = c.Nombre;
                    ws.Cells[i, 3].Value = c.DocumentoCompleto;
                    ws.Cells[i, 4].Value = c.Empresa.NombreFantasia;

                    var rowsToMerge = 0;

                    foreach (var r in c.UltimoRegistroCapacitacionPorCurso(CursoID))
                    {
                        ws.Cells[i, 5].Value = r.Jornada.JornadaIdentificacionCompleta;
                        ws.Cells[i, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[i, 5].Style.Fill.BackgroundColor.SetColor(Color.FromName(r.Jornada.Curso.ColorDeFondo));

                        ws.Cells[i, 6].Value = r.Jornada.Instructor.NombreCompleto;
                        ws.Cells[i, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[i, 6].Style.Fill.BackgroundColor.SetColor(Color.FromName(r.Jornada.Curso.ColorDeFondo));

                        rowsToMerge++;
                        i++;
                    }

                    //se hace un merge para que los datos del capacitado abarquen los rows de los datos de los últimos cursos y para setear el background color
                    if (rowsToMerge > 1)
                    {
                        var rowMergeStart = i - rowsToMerge;
                        var rowMergeEnd = i - 1;

                        for (int col = 1; col <= 4; col++)
                        {
                            if (bgColor != Color.White) //blanco es el color del renglón por defecto, por lo que no es necesario hacer nada si corresponde ese color
                            {
                                ws.Cells[rowMergeStart, col, rowMergeEnd, col].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                ws.Cells[rowMergeStart, col, rowMergeEnd, col].Style.Fill.BackgroundColor.SetColor(bgColor);
                            }

                            ws.Cells[rowMergeStart, col, rowMergeEnd, col].Merge = true;
                            ws.Cells[rowMergeStart, col, rowMergeEnd, col].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        }

                        //se seleccionan las celdas con los datos del capacitado y las celdas con los registros de capacitación
                        ws.Cells[rowMergeStart, 1, rowMergeEnd, 6].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                    }
                    else //se selecciona el único renglón del capacitado para setear el background color.
                    {
                        if (bgColor != Color.White)
                        {
                            ws.Cells[i - 1, 1, i - 1, 4].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            ws.Cells[i - 1, 1, i - 1, 4].Style.Fill.BackgroundColor.SetColor(bgColor);
                        }

                        ws.Cells[i - 1, 1, i - 1, 6].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                    }

                    bgColor = bgColor == Color.White ? Color.WhiteSmoke : Color.White;

                    if (c.UltimoRegistroCapacitacionPorCurso(CursoID).Count() == 0)
                        i++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = "capacitados.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }
    }
}
