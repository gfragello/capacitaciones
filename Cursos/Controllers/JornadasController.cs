using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Cursos.Models;
using OfficeOpenXml;
using System.IO;
using System.Drawing;
using PagedList;

namespace Cursos.Controllers
{
    [Authorize(Roles = "Administrador,AdministradorExterno")]
    public class JornadasController : Controller
    {
        private CursosDbContext db = new CursosDbContext();

        // GET: Jornadas
        public ActionResult Index(int? currentCursoID, int? CursoID,
                                  DateTime? currentFechaInicio, DateTime? fechaInicio,
                                  DateTime? currentFechaFin, DateTime? fechaFin,
                                  bool? currentCreadasOtrosUsuarios, bool? creadasOtrosUsuarios,
                                  int? page)
        {
            if (CursoID != null)
                page = 1;
            else
            {
                if (currentCursoID == null)
                    currentCursoID = -1;

                CursoID = currentCursoID;
            }

            ViewBag.CurrentCursoID = CursoID;

            if (fechaInicio != null)
                page = 1;
            else
                fechaInicio = currentFechaInicio;

            ViewBag.CurrentFechaInicio = fechaInicio;

            if (fechaFin != null)
                page = 1;
            else
                fechaFin = currentFechaFin;

            ViewBag.CurrentFechaFin = fechaFin;

            if (creadasOtrosUsuarios != null)
                page = 1;
            else
                creadasOtrosUsuarios = currentCreadasOtrosUsuarios;

            ViewBag.CurrentCreadasOtrosUsuarios = creadasOtrosUsuarios;

            List<Curso> cursosDD = db.Cursos.OrderBy(c => c.Descripcion).ToList();
            cursosDD.Insert(0, new Curso { CursoID = -1, Descripcion = "Todos" });
            ViewBag.CursoID = new SelectList(cursosDD, "CursoID", "Descripcion", CursoID);

            var jornadas = db.Jornada.Include(j => j.Curso).Include(j => j.Instructor).Include(j => j.Lugar);

            if (CursoID != -1)
                jornadas = jornadas.Where(j => j.CursoId == CursoID);

            if (fechaInicio != null)
                jornadas = jornadas.Where(j => j.Fecha >= fechaInicio.Value);

            if (fechaFin != null)
                jornadas = jornadas.Where(j => j.Fecha <= fechaFin.Value);

            bool motrarCreadasOtrosUsuarios = creadasOtrosUsuarios != null ? creadasOtrosUsuarios.Value : false;

            //si no se aplicó el filtro de ver las jornadas creadas por otro usuarios (está opción solo es válida para el rol Administrador)
            //o si el usuario tiene el rol AdministradorExterno, solo se mostrarán las jornadas creadas por el usuario
            if (!motrarCreadasOtrosUsuarios || User.IsInRole("AdministradorExterno"))
                jornadas = jornadas.Where(j => j.UsuarioModificacion == User.Identity.Name);

            int pageSize = 10;
            int pageNumber = (page ?? 1);

            jornadas = jornadas.OrderByDescending(j => j.Fecha).ThenByDescending(j => j.Hora);

            return View(jornadas.ToPagedList(pageNumber, pageSize));
        }

        // GET: Jornadas/Details/5
        public ActionResult Details(int? id, bool? exportarExcel)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Jornada jornada = db.Jornada.Find(id);
            if (jornada == null)
            {
                return HttpNotFound();
            }

            bool exportar = exportarExcel != null ? (bool)exportarExcel : false;

            if (!exportar)
                return View(jornada);
            else
                return ExportDataExcel(jornada);
        }

        // GET: Jornadas/Create
        public ActionResult Create()
        {
            ViewBag.CursoId = new SelectList(db.Cursos, "CursoID", "Descripcion");
            ViewBag.InstructorId = new SelectList(db.Instructores.Where(i => i.Activo == true), "InstructorID", "NombreCompleto");
            ViewBag.LugarID = new SelectList(db.Lugares, "LugarID", "NombreLugar");
            return View();
        }

        // POST: Jornadas/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "JornadaID,Fecha,CursoId,LugarID,InstructorId,Hora")] Jornada jornada)
        {
            if (ModelState.IsValid)
            {
                jornada.SetearAtributosControl();

                db.Jornada.Add(jornada);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.CursoId = new SelectList(db.Cursos, "CursoID", "Descripcion", jornada.CursoId);
            ViewBag.InstructorId = new SelectList(db.Instructores.Where(i => i.Activo == true), "InstructorID", "NombreCompleto", jornada.InstructorId);
            ViewBag.LugarID = new SelectList(db.Lugares, "LugarID", "NombreLugar", jornada.LugarID);
            return View(jornada);
        }

        // GET: Jornadas/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Jornada jornada = db.Jornada.Find(id);
            if (jornada == null)
            {
                return HttpNotFound();
            }

            if (jornada.PuedeModificarse())
            {
                ViewBag.CursoId = new SelectList(db.Cursos, "CursoID", "Descripcion", jornada.CursoId);
                ViewBag.InstructorId = new SelectList(db.Instructores.Where(i => i.Activo == true), "InstructorID", "NombreCompleto", jornada.InstructorId);
                ViewBag.LugarID = new SelectList(db.Lugares, "LugarID", "NombreLugar", jornada.LugarID);
                return View(jornada);
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }
        }

        // POST: Jornadas/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "JornadaID,Fecha,CursoId,LugarID,InstructorId,Hora")] Jornada jornada)
        {
            if (ModelState.IsValid)
            {
                jornada.SetearAtributosControl();

                db.Entry(jornada).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");
            }
            ViewBag.CursoId = new SelectList(db.Cursos, "CursoID", "Descripcion", jornada.CursoId);
            ViewBag.InstructorId = new SelectList(db.Instructores.Where(i => i.Activo == true), "InstructorID", "NombreCompleto", jornada.InstructorId);
            ViewBag.LugarID = new SelectList(db.Lugares, "LugarID", "NombreLugar", jornada.LugarID);
            return View(jornada);
        }

        // GET: Jornadas/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Jornada jornada = db.Jornada.Find(id);
            if (jornada == null)
            {
                return HttpNotFound();
            }

            if (jornada.PuedeModificarse())
                return View(jornada);
            else
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
        }

        // POST: Jornadas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Jornada jornada = db.Jornada.Find(id);
            db.Jornada.Remove(jornada);
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

        private ActionResult ExportDataExcel(Jornada j)
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Jornada");

                ws.Cells[1, 1].Value = "Curso";
                ws.Cells[1, 2].Value = j.Curso.Descripcion;

                ws.Cells[2, 1].Value = "Instructor";
                ws.Cells[2, 2].Value = j.Instructor.NombreCompleto;

                ws.Cells[3, 1].Value = "Lugar";
                ws.Cells[3, 2].Value = j.Lugar.NombreLugar;

                ws.Cells[4, 1].Value = "Fecha";
                ws.Cells[4, 2].Value = j.Fecha.ToShortDateString();

                ws.Cells[5, 1].Value = "Hora";
                ws.Cells[5, 2].Value = j.Hora;

                ws.Cells[1, 1, 5, 1].Style.Font.Bold = true;

                const int rowInicial = 7;
                int i = rowInicial + 1;

                ws.Cells[rowInicial, 1].Value = "Documento";
                ws.Cells[rowInicial, 2].Value = "Nombre";
                ws.Cells[rowInicial, 3].Value = "Empresa";
                ws.Cells[rowInicial, 4].Value = "Nota previa";
                ws.Cells[rowInicial, 5].Value = "Nota";

                ws.Cells[rowInicial, 1, rowInicial, 5].Style.Font.Bold = true;

                var bgColor = Color.White;

                foreach (var r in j.RegistrosCapacitacion)
                {
                    ws.Cells[i, 1].Value = r.Capacitado.DocumentoCompleto;
                    ws.Cells[i, 2].Value = r.Capacitado.NombreCompleto;
                    ws.Cells[i, 3].Value = r.Capacitado.Empresa.NombreFantasia;
                    ws.Cells[i, 4].Value = r.NotaPrevia;
                    ws.Cells[i, 5].Value = r.Nota;

                    if (bgColor != Color.White) //blanco es el color del renglón por defecto, por lo que no es necesario hacer nada si corresponde ese color
                    {
                        ws.Cells[i, 1, i, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[i, 1, i, 5].Style.Fill.BackgroundColor.SetColor(bgColor);
                    }

                    ws.Cells[i, 1, i, 5].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    bgColor = bgColor == Color.White ? Color.WhiteSmoke : Color.White;
                    i++;
                }

                /*
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
                */
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = String.Format("{0}.xlsx", j.JornadaIdentificacionCompleta);
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }
    }
}
