using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Cursos.Models;
using Cursos.Models.Enums;
using PagedList;
using OfficeOpenXml;
using System.IO;
using System.Drawing;
using Cursos.Helpers;
using Cursos.Helpers.EnvioOVAL;

namespace Cursos.Controllers
{
    [Authorize(Roles = "Administrador,AdministradorExterno,InscripcionesExternas,InstructorExterno")]
    public class RegistrosCapacitacionController : Controller
    {
        private CursosDbContext db = new CursosDbContext();

        // GET: RegistrosCapacitacion
        public ActionResult Index(int? currentCursoID, int? cursoID,
                                  DateTime? currentFechaInicio, DateTime? fechaInicio,
                                  DateTime? currentFechaFin, DateTime? fechaFin,
                                  int? currentNotaDesde, int? notaDesde,
                                  int? currentNotaHasta, int? notaHasta,
                                  int? page, bool? exportarExcel)
        {
            if (cursoID != null) //si el parámetro vino con algún valor es porque se presionó buscar y se resetea la página a 1
                page = 1;
            else
            {
                if (currentCursoID == null)
                    currentCursoID = -1;

                cursoID = currentCursoID;
            }

            ViewBag.CurrentCursoID = cursoID;

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

            if (notaDesde != null)
                page = 1;
            else
                notaDesde = currentNotaDesde;

            ViewBag.CurrentNotaDesde = notaDesde;

            if (notaHasta != null) 
                page = 1;
            else
                notaHasta = currentNotaHasta;

            ViewBag.CurrentNotaHasta = notaHasta;

            var registrosCapacitacion = db.RegistroCapacitacion.Include(r => r.Capacitado).Include(r => r.Jornada).AsQueryable();

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

            int pageSize = 10;
            int pageNumber = (page ?? 1);

            ViewBag.TotalRegistrosCapacitacion = registrosCapacitacion.Count();

            bool exportar = exportarExcel != null ? (bool)exportarExcel : false;

            if (!exportar)
                return View(registrosCapacitacion.ToPagedList(pageNumber, pageSize));
            else
                return ExportDataExcel(registrosCapacitacion.ToList(), cursoID, fechaInicio, fechaFin, notaDesde, notaHasta);
        }

        // GET: RegistrosCapacitacion/IndexOVAL
        public ActionResult IndexOVAL(int? currentEstadoEnvioOVAL, int? EstadoEnvioOVAL, 
                                      bool? paginarResultados, int? page,
                                      bool? iniciarBusqueda, bool? exportarExcel)
        {
            bool paginar;
            int pageSize;
            int pageNumber;

            paginar = paginarResultados != null ? paginarResultados.Value : false;
            pageNumber = page != null ? page.Value : 1;
            iniciarBusqueda = iniciarBusqueda != null ? iniciarBusqueda : false;

            if (iniciarBusqueda.Value)
                pageNumber = 1;

            /*
            if (page == null)
            {
                paginar = false;
                pageNumber = 1;
            }
            else
            {
                paginar = true;
                pageNumber = page.Value;
            }
            */

            /*
            if (EstadoEnvioOVAL != null)
                page = 1;
            else
            {
                if (currentEstadoEnvioOVAL == null)
                    currentEstadoEnvioOVAL = -1; //(int) EstadosEnvioOVAL.Rechazado;

                EstadoEnvioOVAL = currentEstadoEnvioOVAL;
            }
            */

            if (EstadoEnvioOVAL == null)
            {
                if (currentEstadoEnvioOVAL == null)
                    currentEstadoEnvioOVAL = -1; //(int) EstadosEnvioOVAL.Rechazado;

                EstadoEnvioOVAL = currentEstadoEnvioOVAL;
            }

            ViewBag.CurrentEstadoEnvioOVAL = EstadoEnvioOVAL;

            ViewBag.EstadoEnvioOVAL = RegistroCapacitacionHelper.GetInstance().ObtenerEstadoEnvioOvalSelectList(EstadoEnvioOVAL.Value);

            var registrosCapacitacion = db.RegistroCapacitacion.Include(r => r.Capacitado);

            if (EstadoEnvioOVAL == -1)
                registrosCapacitacion = registrosCapacitacion.Where(r => r.EnvioOVALEstado != EstadosEnvioOVAL.NoEnviar);
            else
                registrosCapacitacion = registrosCapacitacion.Where(r => r.EnvioOVALEstado == (EstadosEnvioOVAL)EstadoEnvioOVAL);

            registrosCapacitacion = registrosCapacitacion.OrderByDescending(r => r.Jornada.Fecha);

            bool exportar = exportarExcel != null ? (bool)exportarExcel : false;

            if (!exportar)
            {
                if (paginar)
                    pageSize = 10;
                else
                    pageSize = registrosCapacitacion.Count() > 0 ? registrosCapacitacion.Count() : 1;

                return View(registrosCapacitacion.ToPagedList(pageNumber, pageSize));
            }
            else
                return ExportDataOVALExcel(registrosCapacitacion.ToList());
        }

        public ActionResult IndexLogs_enviosOVAL(int? page)
        {
            bool paginar;
            int pageSize;
            int pageNumber;

            if (page == null)
            {
                paginar = false;
                pageNumber = 1;
            }
            else
            {
                paginar = true;
                pageNumber = page.Value;
            }

            var pathsArchivosLogsEnviosOVAL = LogHelper.GetInstance().GetModuleLogFiles("enviosOVAL");

            if (paginar)
                pageSize = 10;
            else
                pageSize = pathsArchivosLogsEnviosOVAL.Count() > 0 ? pathsArchivosLogsEnviosOVAL.Count() : 1;

            return View(pathsArchivosLogsEnviosOVAL.ToPagedList(pageNumber, pageSize));
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
            var jornadas = db.Jornada.Where(j => j.UsuarioModificacion == User.Identity.Name);
            ViewBag.JornadaID = new SelectList(jornadas.OrderByDescending(j => j.Fecha).ThenByDescending(j => j.Hora).ToList(), "JornadaID", "JornadaIdentificacionCompleta");

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

            //se setean nota y fecha de vencimiento con un valor por defecto
            RegistroCapacitacion r = new RegistroCapacitacion();
            r.Nota = 0;

            return View("Create", r);
        }

        // POST: RegistrosCapacitacion/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateWithCapacitado([Bind(Include = "RegistroCapacitacionID,Aprobado,Nota,NotaPrevia,Estado,JornadaID,CapacitadoID,FechaVencimiento")] RegistroCapacitacion registroCapacitacion)
        {
            if (ModelState.IsValid)
            {
                registroCapacitacion.SetearAtributosControl();

                var jornada = db.Jornada.Where(j => j.JornadaID == registroCapacitacion.JornadaID).FirstOrDefault();

                if (jornada == null)
                    return HttpNotFound();

                if (jornada.PermiteEnviosOVAL)
                    registroCapacitacion.EnvioOVALEstado = EstadosEnvioOVAL.PendienteEnvio;
                else
                    registroCapacitacion.EnvioOVALEstado = EstadosEnvioOVAL.NoEnviar;

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

            var jornadas = db.Jornada.Where(j => j.UsuarioModificacion == User.Identity.Name);
            ViewBag.JornadaID = new SelectList(jornadas.OrderByDescending(j => j.Fecha).ThenByDescending(j => j.Hora).ToList(), "JornadaID", "JornadaIdentificacionCompleta", registroCapacitacion.JornadaID);

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

            RegistroCapacitacion registroCapacitacion = db.RegistroCapacitacion.Where(r => r.RegistroCapacitacionID == id).Include(r => r.Capacitado).FirstOrDefault();

            if (registroCapacitacion == null)
            {
                return HttpNotFound();
            }
            ViewBag.CapacitadoID = new SelectList(db.Capacitados, "CapacitadoID", "Nombre", registroCapacitacion.CapacitadoID);

            ViewBag.JornadaID = registroCapacitacion.JornadaID;
            ViewBag.JornadaIdentificacionCompleta = registroCapacitacion.Jornada.JornadaIdentificacionCompleta;

            // get the previous url and store it with view model
            ViewBag.PreviousUrl = System.Web.HttpContext.Current.Request.UrlReferrer;

            return View(registroCapacitacion);
        }

        // POST: RegistrosCapacitacion/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RegistroCapacitacionID,Aprobado,Nota,NotaPrevia,Estado,JornadaID,CapacitadoID,FechaVencimiento,EnvioOVALEstado")] RegistroCapacitacion registroCapacitacion, 
                                  string PreviousUrl)
        {
            if (ModelState.IsValid)
            {
                registroCapacitacion.SetearAtributosControl();

                db.Entry(registroCapacitacion).State = EntityState.Modified;
                db.SaveChanges();

                if (!String.IsNullOrEmpty(PreviousUrl))
                    return Redirect(PreviousUrl);
                else
                    return RedirectToAction("Details", new { id = registroCapacitacion.RegistroCapacitacionID });
            }
            ViewBag.CapacitadoID = new SelectList(db.Capacitados, "CapacitadoID", "Nombre", registroCapacitacion.CapacitadoID);

            var jornadas = db.Jornada.Where(j => j.UsuarioModificacion == User.Identity.Name);
            ViewBag.JornadaID = new SelectList(jornadas.OrderByDescending(j => j.Fecha).ThenByDescending(j => j.Hora).ToList(), "JornadaID", "JornadaIdentificacionCompleta");

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
            if (registroCapacitacion.PuedeModificarse())
                return View(registroCapacitacion);
            else
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
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
            return db.Jornada.Find(JornadaId).ObtenerFechaVencimiento();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private ActionResult ExportDataExcel(List<RegistroCapacitacion> registrosCapacitacion,
                                            int? cursoID,
                                            DateTime? fechaInicio,
                                            DateTime? fechaFin,
                                            int? notaDesde,
                                            int? notaHasta)
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Registros");

                int i = 1;

                if (cursoID != null)
                {
                    var c = db.Cursos.Find(cursoID);

                    if (c != null)
                    {
                        ws.Cells[i, 2].Value = string.Format("Curso: {0}", c.Descripcion);
                        i++;
                    }
                }

                string filtroFechaAplicado = null;

                if (fechaInicio != null && fechaFin != null)
                {
                    filtroFechaAplicado = string.Format("{0} al {1}", fechaInicio.Value.ToShortDateString(), fechaFin.Value.ToShortDateString());
                }
                else if (fechaInicio != null)
                {
                    filtroFechaAplicado = string.Format("Desde el {0}", fechaInicio.Value.ToShortDateString());
                }
                else if (fechaFin != null)
                {
                    filtroFechaAplicado = string.Format("Hasta el {0}", fechaFin.Value.ToShortDateString());
                }

                if (filtroFechaAplicado != null)
                {
                    ws.Cells[i, 2].Value = string.Format("Fecha: {0}", filtroFechaAplicado);
                    i++;
                }

                string filtroNotaAplicado = null;

                if (notaDesde != null && notaHasta != null)
                {
                    filtroNotaAplicado = string.Format("{0} a {1}", notaDesde.Value.ToString(), notaHasta.Value.ToString());
                }
                else if (notaDesde != null)
                {
                    filtroNotaAplicado = string.Format("Desde {0}", notaDesde.ToString());
                }
                else if (notaHasta != null)
                {
                    filtroNotaAplicado = string.Format("Hasta {0}", notaHasta.ToString());
                }

                if (filtroNotaAplicado != null)
                {
                    ws.Cells[i, 2].Value = string.Format("Nota: {0}", filtroNotaAplicado);
                    i++;
                }

                int rowHeader = ++i;
                i = rowHeader + 1;

                const int colDocumento = 1;
                const int colNombre = 2;
                const int colApellido = 3;
                const int colFechaNacimiento = 4;
                const int colEdad = 5;
                const int colEmpresa = 6;
                const int colCurso = 7;
                const int colJornada = 8;
                const int colLugar = 9;
                const int colInstructor = 10;
                const int colNota = 11;

                ws.Cells[rowHeader, colDocumento].Value = "Documento";
                ws.Cells[rowHeader, colNombre].Value = "Nombre";
                ws.Cells[rowHeader, colApellido].Value = "Apellido";
                ws.Cells[rowHeader, colFechaNacimiento].Value = "Fec. Nac.";
                ws.Cells[rowHeader, colEdad].Value = "Edad";
                ws.Cells[rowHeader, colEmpresa].Value = "Empresa";
                ws.Cells[rowHeader, colCurso].Value = "Curso";
                ws.Cells[rowHeader, colJornada].Value = "Jornada";
                ws.Cells[rowHeader, colLugar].Value = "Lugar";
                ws.Cells[rowHeader, colInstructor].Value = "Instructor";
                ws.Cells[rowHeader, colNota].Value = "Nota";

                var bgColor = Color.White;

                foreach (var r in registrosCapacitacion)
                {
                    ws.Cells[i, colDocumento].Value = r.Capacitado.DocumentoCompleto;
                    ws.Cells[i, colNombre].Value = r.Capacitado.Nombre;
                    ws.Cells[i, colApellido].Value = r.Capacitado.Apellido;
                    ws.Cells[i, colFechaNacimiento].Value = r.Capacitado.Fecha !=null ? r.Capacitado.Fecha.Value.ToShortDateString() : string.Empty;
                    ws.Cells[i, colEdad].Value =  r.Capacitado.ObtenerEdadFecha(r.Jornada.Fecha);
                    ws.Cells[i, colEmpresa].Value = r.Capacitado.Empresa.NombreFantasia;
                    ws.Cells[i, colCurso].Value = r.Jornada.Curso.Descripcion;
                    ws.Cells[i, colJornada].Value = r.Jornada.FechaHoraTexto;
                    ws.Cells[i, colLugar].Value = r.Jornada.Lugar.AbrevLugar;
                    ws.Cells[i, colInstructor].Value = r.Jornada.Instructor.NombreCompleto;
                    ws.Cells[i, colNota].Value = r.Nota;

                    //se seleccionan las columnas con datos del capacitado para setear el background color.
                    if (bgColor != Color.White)
                    {
                        ws.Cells[i, colDocumento, i, colNota].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[i, colDocumento, i, colNota].Style.Fill.BackgroundColor.SetColor(bgColor);
                    }

                    //se pone un borde alrededor del renglón
                    ws.Cells[i, colDocumento, i, colNota].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    bgColor = bgColor == Color.White ? Color.WhiteSmoke : Color.White;

                    i++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = "registros.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }

        public ActionResult ObtenerEstadoRegistroCapacitacion(int cursoId, int nota)
        {
            var curso = db.Cursos.Find(cursoId);

            if (curso == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            EstadosRegistroCapacitacion estadoRet = EstadosRegistroCapacitacion.Aprobado;

            if (nota < curso.PuntajeMinimo)
                estadoRet = EstadosRegistroCapacitacion.NoAprobado;

            return Json((int)estadoRet, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CalificarRegistroNota(int registroCapacitacionId, int nota)
        {
            var datosActualizarNotaError = new
            {
                NotaActualizada = false,
                LabelEstado = string.Empty
            };

            var registroCapacitacion = db.RegistroCapacitacion.Find(registroCapacitacionId);

            if (registroCapacitacion == null)
                return Json(datosActualizarNotaError, JsonRequestBehavior.AllowGet);

            if (registroCapacitacion.Calificar(nota))
                db.SaveChanges();
            else
                return Json(datosActualizarNotaError, JsonRequestBehavior.AllowGet);

            var datosActualizarNotaSuccess = new
            {
                NotaActualizada = true,
                LabelEstado = RegistroCapacitacionHelper.GetInstance().ObtenerLabelEstado(registroCapacitacion)
            };

            return Json(datosActualizarNotaSuccess, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CalificarRegistro(int registroCapacitacionId, bool aprobado)
        {
            var datosActualizarCalificacionError = new
            {
                CalificacionActualizada = false,
                LabelEstado = string.Empty
            };

            var registroCapacitacion = db.RegistroCapacitacion.Find(registroCapacitacionId);

            if (registroCapacitacion == null)
                return Json(datosActualizarCalificacionError, JsonRequestBehavior.AllowGet);

            if (registroCapacitacion.Calificar(aprobado))
                db.SaveChanges();
            else
                return Json(datosActualizarCalificacionError, JsonRequestBehavior.AllowGet);

            var datosActualizarCalificacionSuccess = new
            {
                CalificacionActualizada = true,
                LabelEstado = RegistroCapacitacionHelper.GetInstance().ObtenerLabelEstado(registroCapacitacion)
            };

            return Json(datosActualizarCalificacionSuccess, JsonRequestBehavior.AllowGet);
        }

        public ActionResult BorrarCalificacionRegistro(int registroCapacitacionId)
        {
            var datosActualizarCalificacionError = new
            {
                CalificacionActualizada = false,
                LabelEstado = string.Empty
            };

            var registroCapacitacion = db.RegistroCapacitacion.Find(registroCapacitacionId);

            if (registroCapacitacion == null)
                return Json(datosActualizarCalificacionError, JsonRequestBehavior.AllowGet);

            registroCapacitacion.BorrarCalificacion();
            db.SaveChanges();


            var datosActualizarCalificacionSuccess = new
            {
                CalificacionActualizada = true,
                LabelEstado = RegistroCapacitacionHelper.GetInstance().ObtenerLabelEstado(registroCapacitacion)
            };

            return Json(datosActualizarCalificacionSuccess, JsonRequestBehavior.AllowGet);
        }

        public ActionResult EliminarRegistroCapacitacion(int registroCapacitacionId)
        {
            RegistroCapacitacion registroCapacitacion = db.RegistroCapacitacion.Find(registroCapacitacionId);
            db.RegistroCapacitacion.Remove(registroCapacitacion);
            db.SaveChanges();

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult EnviarDatosRegistroOVAL(int registroCapacitacionId)
        {
            int totalAceptados = 0;
            int totalRechazados = 0;

            bool todosEnviadosOK = EnvioOVALHelper.GetInstance().EnviarDatosRegistroOVAL(registroCapacitacionId, ref totalAceptados, ref totalRechazados);
            
            var resultadoEnviarDatosOVAL = new
            {
                todosEnviadosOK = todosEnviadosOK,
                totalAceptados = totalAceptados,
                totalRechazados = totalRechazados
            };

            return Json(resultadoEnviarDatosOVAL, JsonRequestBehavior.AllowGet);
        }

        public ActionResult EnviarDatosRegistosOVALRechazados()
        {
            var registrosCapacitacionRechazadosIDs = db.RegistroCapacitacion.Where(r => r.EnvioOVALEstado == EstadosEnvioOVAL.Rechazado).Select(r => r.RegistroCapacitacionID).ToList();

            int totalAceptados = 0;
            int totalRechazados = 0;

            bool todosEnviadosOK = EnvioOVALHelper.GetInstance().EnviarDatosListaRegistros(registrosCapacitacionRechazadosIDs, ref totalAceptados, ref totalRechazados);

            var resultadoEnviarDatosOVAL = new
            {
                todosEnviadosOK = todosEnviadosOK,
                totalAceptados = totalAceptados,
                totalRechazados = totalRechazados
            };

            return Json(resultadoEnviarDatosOVAL, JsonRequestBehavior.AllowGet);
        }

        private ActionResult ExportDataOVALExcel(List<RegistroCapacitacion> registrosCapacitacion)
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Registros");

                const int rowInicial = 2;
                int i = rowInicial + 1;

                ws.Cells[rowInicial, 1].Value = "Documento";
                ws.Cells[rowInicial, 2].Value = "Nombre";
                ws.Cells[rowInicial, 3].Value = "Empresa";
                ws.Cells[rowInicial, 4].Value = "Jornada";
                ws.Cells[rowInicial, 5].Value = "Estado último envío";
                ws.Cells[rowInicial, 6].Value = "Fecha último envío";
                ws.Cells[rowInicial, 7].Value = "Mensaje último envío";

                //se ponen en negrita los encabezados
                ws.Cells[rowInicial, 1, rowInicial, 7].Style.Font.Bold = true;

                var bgColor = Color.White;

                foreach (var r in registrosCapacitacion)
                {
                    ws.Cells[i, 1].Value = r.Capacitado.Documento;
                    ws.Cells[i, 2].Value = r.Capacitado.NombreCompleto;
                    ws.Cells[i, 3].Value = r.Capacitado.Empresa.NombreFantasia;
                    ws.Cells[i, 4].Value = r.Jornada.JornadaIdentificacionCompleta;
                    ws.Cells[i, 5].Value = r.EnvioOVALEstado.ToString();
                    ws.Cells[i, 6].Value = r.EnvioOVALFechaHora.ToString();
                    ws.Cells[i, 7].Value = r.EnvioOVALMensaje;

                    //se seleccionan las columnas con datos del capacitado para setear el background color.
                    if (bgColor != Color.White)
                    {
                        ws.Cells[i - 1, 1, i - 1, 7].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[i - 1, 1, i - 1, 7].Style.Fill.BackgroundColor.SetColor(bgColor);
                    }

                    //se pone un borde alrededor del renglón del encabezado
                    //ws.Cells[i - 1, 1, i - 1, 7].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    bgColor = bgColor == Color.White ? Color.WhiteSmoke : Color.White;

                    i++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = "estadoRegistrosOVAL.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }

    }
}
