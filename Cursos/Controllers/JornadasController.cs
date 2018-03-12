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
using System.Threading.Tasks;
using Cursos.Models.Enums;

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
        public ActionResult Details(int? id,
                                    bool? exportarExcel,
                                    bool? generarActa)
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

            bool excel = exportarExcel != null ? (bool)exportarExcel : false;
            bool acta = generarActa != null ? (bool)generarActa : false;

            if (excel)
                return ExportDataExcel(jornada);

            if (acta)
                return GenerarActa(jornada);

            return View(jornada);

        }

        // GET: Jornadas/Create - Si se especifica un valor en el parámtero id, se copiaran sus registros de 
        // capacitación a la jornada creada
        public ActionResult Create(int? id)
        {
            ViewBag.CursoId = new SelectList(db.Cursos, "CursoID", "Descripcion");
            ViewBag.InstructorId = new SelectList(db.Instructores.Where(i => i.Activo == true), "InstructorID", "NombreCompleto");
            ViewBag.LugarID = new SelectList(db.Lugares, "LugarID", "NombreLugar");

            ViewBag.JornadaTemplateId = id;

            if (id != null)
            {
                Jornada jornadaTemplate = db.Jornada.Find(id);

                if (jornadaTemplate == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                else
                {
                    List<Capacitado> capacitadosTemplate = new List<Capacitado>();

                    foreach (var r in jornadaTemplate.RegistrosCapacitacion)
                    {
                        capacitadosTemplate.Add(r.Capacitado);
                    }

                    ViewBag.CapacitadosTemplate = capacitadosTemplate;
                }
            }

            Jornada j = new Jornada();
            j.Fecha = DateTime.Now;
            j.CuposDisponibles = true;

            return View(j);
        }

        // POST: Jornadas/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "JornadaID,Fecha,CursoId,LugarID,Direccion,InstructorId,Hora,CuposDisponibles")] Jornada jornada, int? JornadaTemplateId)
        {
            if (ModelState.IsValid)
            {
                jornada.SetearAtributosControl();

                if (JornadaTemplateId != null)
                {
                    Jornada jornadaTemplate = db.Jornada.Find(JornadaTemplateId);

                    if (jornadaTemplate == null)
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

                    foreach (var rc in jornadaTemplate.RegistrosCapacitacion)
                    {
                        var nuevoRC = new RegistroCapacitacion();
                        nuevoRC.SetearAtributosControl();

                        nuevoRC.Jornada = jornada;
                        nuevoRC.Capacitado = rc.Capacitado;
                        nuevoRC.Nota = 0;
                        nuevoRC.Aprobado = true;
                        nuevoRC.FechaVencimiento = jornada.ObtenerFechaVencimiento(true);

                        db.RegistroCapacitacion.Add(nuevoRC);
                    }
                }

                jornada.HoraFormatoNumerico = int.Parse(jornada.HoraSinSeparador);

                db.Jornada.Add(jornada);
                db.SaveChanges();

                if (JornadaTemplateId == null)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    //jornada = db.Jornada.Find(jornada.JornadaID);
                    //return View("Details", jornada);
                    //return RedirectToAction("Details", jornada);
                    return RedirectToAction("Details", "Jornadas", new { id = jornada.JornadaID });
                }
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
        public ActionResult Edit([Bind(Include = "JornadaID,Fecha,CursoId,LugarID,Direccion,InstructorId,Hora,CuposDisponibles")] Jornada jornada)
        {
            if (ModelState.IsValid)
            {
                jornada.SetearAtributosControl();

                jornada.HoraFormatoNumerico = int.Parse(jornada.HoraSinSeparador);

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

        public ActionResult ToggleCuposDisponibles(int? id)
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
                jornada.CuposDisponibles = !jornada.CuposDisponibles;
                db.SaveChanges();

                return RedirectToAction("Details", new { id = id });
            }
            else
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult AgregarRegistroCapacitacion(int jornadaId, int capacitadoId)
        {
            var jornada = db.Jornada.Find(jornadaId);
            var capacitado = db.Capacitados.Find(capacitadoId);

            if (jornada == null || capacitado == null)
                return Json(false, JsonRequestBehavior.AllowGet);

            RegistroCapacitacion registroCapacitacion = new RegistroCapacitacion
            {
                Jornada = jornada,
                Capacitado = capacitado,
                Nota = 0,
                Aprobado = true,
                FechaVencimiento = jornada.ObtenerFechaVencimiento()
            };

            registroCapacitacion.SetearAtributosControl();
            db.RegistroCapacitacion.Add(registroCapacitacion);

            db.SaveChanges();

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SetearRegistroCapacitacionEstado(int jornadaId, EstadosRegistroCapacitacion estado)
        {
            var jornada = db.Jornada.Find(jornadaId);

            if (jornada == null)
                return Json(false, JsonRequestBehavior.AllowGet);

            foreach (var r in jornada.RegistrosCapacitacion)
            {
                r.Estado = estado;
            }

            db.SaveChanges();

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult ObtenerRegistrosCapacitacionJornada(int jornadaId)
        {
            var jornada = db.Jornada.Find(jornadaId);

            if (jornada == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            return PartialView("_ListRegistrosCapacitacionPartial", jornada.RegistrosCapacitacion.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AgregarActa(int? jornadaId, HttpPostedFileBase upload)
        {
            if (jornadaId != null && upload != null && upload.ContentLength > 0)
            {
                var jornada = db.Jornada.Find(jornadaId);

                if (jornada == null)
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);

                string nombreArchivo = String.Format("Acta_{0}{1}",
                                                     jornada.JornadaID.ToString(),
                                                     System.IO.Path.GetExtension(upload.FileName));

                var pathActa = new PathArchivo
                {
                    NombreArchivo = nombreArchivo,
                    TipoArchivo = Models.Enums.TiposArchivo.FotoCapacitado
                };
                jornada.PathArchivo = pathActa;

                var path = Path.Combine(Server.MapPath("~/Images/Actas/"), pathActa.NombreArchivo);
                upload.SaveAs(path);

                db.SaveChanges();

                return RedirectToAction("Details", new { id = jornada.JornadaID });
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarActa(int? jornadaId)
        {
            if (jornadaId == null)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);

            var jornada = db.Jornada.Find(jornadaId);

            if (jornada == null)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);

            var pathActa = jornada.PathArchivo;
            jornada.PathArchivo = null;

            string pathCompleto = Request.MapPath("~/Images/Actas/" + pathActa.NombreArchivo);
            if (System.IO.File.Exists(pathCompleto))
            {
                System.IO.File.Delete(pathCompleto);
            }

            db.PathArchivos.Remove(pathActa);

            db.SaveChanges();

            return RedirectToAction("Details", new { id = jornada.JornadaID });
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

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = String.Format("{0}.xlsx", j.JornadaIdentificacionCompleta);
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }

        private ActionResult GenerarActa(Jornada j)
        {
            const int heightSeparaciónCabezal = 40;

            const int colLabelCapacitacion = 2;
            const int colCurso = 3;

            const int colLabelLugar = 5;
            const int colLugar = 6;

            const int colFecha = 8;

            const int colInstructor = 8;
            const int rowInstructor = 30;

            const int colCoordinador = 8;
            const int rowCoordinador = 32;

            const int heightDetalleCapacitado = 30;

            const int rowHeaderCapacitados = 3;

            const int colOrdinal = 1;
            const int colNombre = 2;
            const int colApellido = 3;
            const int colTipoDocumento = 4;
            const int colDocumento = 5;
            const int colFechaNacimiento = 6;
            const int colEmpresa = 7;
            const int colFirma = 8;
            const int colAprobado = 9;
            const int colNota = 10;

            const int colRangoPuntajesDesde = 9;
            const int colRangoPuntajeHasta = 10;

            const int widthOrdinal = 8;
            const int widthFirma = 27;

            //constantes para implementación de páginado
            const int totalCapacitadosPorHoja = 25;
            int totalCapacitadosHojaActual = 0;

            const int colPagina = 10;

            const int colFirmas = 8;

            const int rowFirmaInstructor = 39;
            const int rowFirmaResponsable = 44;

            int wsActual = 1;

            using (ExcelPackage package = new ExcelPackage())
            {
                string nombreWSBase = j.Curso.Descripcion;
                string nombreWS = nombreWSBase;

                if (j.RegistrosCapacitacion.Count > totalCapacitadosPorHoja)
                    nombreWS = string.Format("{0} - {1}", nombreWSBase, wsActual.ToString());

                var ws = package.Workbook.Worksheets.Add(nombreWS);

                int ordinal = 1;
                int rowActual = rowHeaderCapacitados;

                foreach (var r in j.RegistrosCapacitacion)
                {
                    if (totalCapacitadosHojaActual >= totalCapacitadosPorHoja)
                    {
                        totalCapacitadosHojaActual = 0;
                        wsActual++;

                        rowActual = rowHeaderCapacitados;

                        ws = package.Workbook.Worksheets.Add(string.Format("{0} - {1}", nombreWSBase, wsActual.ToString()));
                    }

                    rowActual++;

                    ws.Cells[rowActual, colOrdinal].Value = ordinal;
                    ws.Cells[rowActual, colOrdinal].Style.Font.Bold = true;
                    ws.Cells[rowActual, colOrdinal].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    ws.Cells[rowActual, colNombre].Value = r.Capacitado.Nombre;
                    ws.Cells[rowActual, colApellido].Value = r.Capacitado.Apellido;
                    ws.Cells[rowActual, colTipoDocumento].Value = r.Capacitado.TipoDocumento.Abreviacion;
                    ws.Cells[rowActual, colDocumento].Value = r.Capacitado.Documento;

                    if (r.Capacitado.Fecha != null)
                        ws.Cells[rowActual, colFechaNacimiento].Value = ((DateTime)r.Capacitado.Fecha).ToShortDateString();

                    ws.Cells[rowActual, colEmpresa].Value = r.Capacitado.Empresa.NombreFantasia;

                    ws.Row(rowActual).Height = heightDetalleCapacitado;

                    ordinal++;
                    totalCapacitadosHojaActual++;
                }

                while (totalCapacitadosHojaActual < totalCapacitadosPorHoja)
                {
                    rowActual++;

                    ws.Cells[rowActual, colOrdinal].Value = ordinal;
                    ws.Cells[rowActual, colOrdinal].Style.Font.Bold = true;
                    ws.Cells[rowActual, colOrdinal].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    ws.Row(rowActual).Height = heightDetalleCapacitado;

                    ordinal++;
                    totalCapacitadosHojaActual++;
                }

                int paginaActual = 1;

                //se agregan cabezal y pie de página
                foreach (var wsFormat in package.Workbook.Worksheets)
                {
                    //Cabezal ////////////////////////////////////////////////////////////////////////////
                    //se setea el label Capacitación, donde se indica además el curso
                    wsFormat.Cells[1, colLabelCapacitacion].Value = "Capacitación: ";
                    wsFormat.Cells[1, colLabelCapacitacion].Style.Font.Bold = true;

                    wsFormat.Cells[1, colCurso].Value = j.Curso.Descripcion;
                    wsFormat.Cells[1, colCurso].Style.Font.Bold = true;
                    wsFormat.Cells[1, colCurso].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    wsFormat.Cells[1, colCurso].Style.Font.Color.SetColor(Color.White);
                    wsFormat.Cells[1, colCurso].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    wsFormat.Cells[1, colCurso].Style.Fill.BackgroundColor.SetColor(Color.FromName(j.Curso.ColorDeFondo));

                    wsFormat.Cells[1, colLabelCapacitacion, 1, colCurso].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    //se muestra la información del lugar
                    wsFormat.Cells[1, colLabelLugar].Value = "Lugar: ";
                    wsFormat.Cells[1, colLabelLugar].Style.Font.Bold = true;

                    wsFormat.Cells[1, colLugar].Value = j.Lugar.NombreLugar;
                    wsFormat.Cells[1, colLugar].Style.Font.Bold = true;

                    wsFormat.Cells[1, colLabelLugar, 1, colLugar].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    //se muestra la fecha
                    wsFormat.Cells[1, colFecha].Value = String.Format("{0}: {1}", "Fecha", j.Fecha.ToShortDateString());
                    wsFormat.Cells[1, colFecha].Style.Font.Bold = true;
                    wsFormat.Cells[1, colFecha].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    wsFormat.Row(2).Height = heightSeparaciónCabezal;

                    //se setea el cabezal de los capacitados
                    wsFormat.Cells[rowHeaderCapacitados, colNombre].Value = "Nombre";
                    wsFormat.Cells[rowHeaderCapacitados, colApellido].Value = "Apellido";
                    wsFormat.Cells[rowHeaderCapacitados, colTipoDocumento].Value = "Tipo";
                    wsFormat.Cells[rowHeaderCapacitados, colDocumento].Value = "Documento";
                    wsFormat.Cells[rowHeaderCapacitados, colFechaNacimiento].Value = "Fecha Nac";
                    wsFormat.Cells[rowHeaderCapacitados, colEmpresa].Value = "Empresa";
                    wsFormat.Cells[rowHeaderCapacitados, colFirma].Value = "Firma";

                    wsFormat.Cells[rowHeaderCapacitados, colRangoPuntajesDesde, rowHeaderCapacitados, colRangoPuntajesDesde].Value = String.Format("Max {0} \r\nMín {1}", j.Curso.PuntajeMaximo.ToString(), j.Curso.PuntajeMinimo.ToString());
                    wsFormat.Cells[rowHeaderCapacitados, colRangoPuntajesDesde, rowHeaderCapacitados, colRangoPuntajeHasta].Merge = true;
                    //esto permite que en la celda se tengan en cuenta los caracteres de escape de nueva línea
                    wsFormat.Cells[rowHeaderCapacitados, colRangoPuntajesDesde, rowHeaderCapacitados, colRangoPuntajeHasta].Style.WrapText = true;

                    //se setea el estilo del cabezal de los capacitados
                    var cellsHeaderCapacitados = wsFormat.Cells[rowHeaderCapacitados, colOrdinal, rowHeaderCapacitados, colNota];

                    cellsHeaderCapacitados.Style.Font.Bold = true;
                    cellsHeaderCapacitados.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    wsFormat.Row(rowHeaderCapacitados).Height = heightDetalleCapacitado;

                    //Cuerpo y Pie de página ////////////////////////////////////////////////////////////////////////////
                    var rangoContenido = wsFormat.Cells[rowHeaderCapacitados, colOrdinal, rowActual, colNota];

                    rangoContenido.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                    rangoContenido.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    rangoContenido.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    rangoContenido.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    rangoContenido.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                    var rangoContenidoTipoDocumento = wsFormat.Cells[rowHeaderCapacitados + 1, colTipoDocumento, rowActual - 1, colTipoDocumento];
                    rangoContenidoTipoDocumento.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    var rangoFechaNacimiento = wsFormat.Cells[rowHeaderCapacitados + 1, colFechaNacimiento, rowActual - 1, colFechaNacimiento];
                    rangoFechaNacimiento.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    //seteo de anchos de columna
                    wsFormat.Cells[wsFormat.Dimension.Address].AutoFitColumns();
                    wsFormat.Column(colFirma).Width = widthFirma;
                    wsFormat.Column(colOrdinal).Width = widthOrdinal;

                    //seteo de los parámetros de impresión
                    wsFormat.PrinterSettings.PaperSize = ePaperSize.A4;
                    wsFormat.PrinterSettings.Orientation = eOrientation.Portrait;
                    wsFormat.PrinterSettings.FitToPage = true;

                    //se muestra el nombre del instructor
                    wsFormat.Cells[rowInstructor, colInstructor].Value = String.Format("{0}: {1}", "Instructor", j.Instructor.NombreCompleto);
                    wsFormat.Cells[rowInstructor, colInstructor].Style.Font.Bold = true;

                    //se muestra el nombre del coordinador
                    wsFormat.Cells[rowCoordinador, colCoordinador].Value = String.Format("{0}: {1}", "Coordinador", "Alejandro Lacruz");
                    wsFormat.Cells[rowCoordinador, colCoordinador].Style.Font.Bold = true;

                    wsFormat.Cells[1, colPagina].Value = string.Format("Página {0}/{1}", paginaActual, package.Workbook.Worksheets.Count.ToString());

                    paginaActual++;
                }

                var wsFinal = package.Workbook.Worksheets.Last();

                //en la última ws se agrega el acta
                AgregarEncuestaActa(wsFinal, "ENCUESTA: Cómo consideraron el curso los asistentes");

                //se agrega el espacio para la firma del instructor
                wsFinal.Cells[rowFirmaInstructor, colFirmas].Value = "Firma Instructor";
                wsFinal.Cells[rowFirmaInstructor, colFirmas].Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                //se agrega el espacio para la firma del instructor
                wsFinal.Cells[rowFirmaResponsable, colFirmas].Value = "Firma Responsable";
                wsFinal.Cells[rowFirmaResponsable, colFirmas].Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = String.Format("ACTA {0} {1}.xlsx", j.Curso.Descripcion, j.FechaFormatoYYYYYMMDD);
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }

        private void AgregarEncuestaActa(ExcelWorksheet ws, string tituloEncuesta)
        {
            const int rowTituloEncuesta = 31;
            const int colTituloEncuesta = 2;

            const int rowInicioEncuesta = 32;

            //se inicializa el título de la encuesta
            var celdaTitulo = ws.Cells[rowTituloEncuesta, colTituloEncuesta];
            celdaTitulo.Value = tituloEncuesta;
            celdaTitulo.Style.Font.Bold = true;

            int rowActual = rowInicioEncuesta;
            ws.Row(rowActual + 1).Height = 30;
            AgregarOpcionEncuesta(ws, "Malo: ", "Malo", rowActual);

            rowActual++;
            ws.Row(rowActual + 1).Height = 30;
            AgregarOpcionEncuesta(ws, "Regular: ", "Regular", rowActual);

            rowActual++;
            ws.Row(rowActual + 1).Height = 30;
            AgregarOpcionEncuesta(ws, "Bueno: ", "Bueno", rowActual);

            rowActual++;
            ws.Row(rowActual + 1).Height = 30;
            AgregarOpcionEncuesta(ws, "Muy Bueno: ", "MuyBueno", rowActual);

            rowActual++;
            ws.Row(rowActual + 1).Height = 30;
            AgregarOpcionEncuesta(ws, "Excelente: ", "Excelente", rowActual);
        }

        private void AgregarOpcionEncuesta(ExcelWorksheet ws, string label, string nombreShape, int rowPos)
        {
            const int colPos = 1;

            const int widthShapeOpcion = 150;
            const int heightShapeOpcion = 30;

            var shapeOpcion = ws.Drawings.AddShape(String.Format("txtOpcion{0}", nombreShape), eShapeStyle.Rect);
            shapeOpcion.SetPosition(rowPos, 0, colPos, 0);
            shapeOpcion.SetSize(widthShapeOpcion, heightShapeOpcion);

            shapeOpcion.Text = label;

            shapeOpcion.Border.Fill.Style = eFillStyle.SolidFill;
            shapeOpcion.Border.LineStyle = OfficeOpenXml.Drawing.eLineStyle.Solid;
            shapeOpcion.Border.Width = 1;
            shapeOpcion.Border.Fill.Color = Color.Black;
            shapeOpcion.Fill.Color = Color.White;
            shapeOpcion.Font.Color = Color.Black;
            shapeOpcion.Font.Bold = true;
        }

        [AllowAnonymous]
        public ActionResult Disponibles()
        {
            var jornadas = db.Jornada.Where(j => j.Fecha >= DateTime.Now).OrderBy(j => j.Fecha).ThenBy(j => j.HoraFormatoNumerico).Include(j => j.Curso).Include(j => j.Instructor).Include(j => j.Lugar);

            return View(jornadas.ToList());
        }   
    }
}