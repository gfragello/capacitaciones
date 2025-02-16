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
using Cursos.Helpers;
using System.Net.Mail;
using Cursos.Helpers.EnvioOVAL;
using Cursos.Models.ViewModels;

namespace Cursos.Controllers
{
    [Authorize(Roles = "Administrador,AdministradorExterno,InscripcionesExternas,InstructorExterno")]
    public class JornadasController : BaseController
    {
        private CursosDbContext db = new CursosDbContext();

        // GET: Jornadas
        public ActionResult Index(int? currentCursoID, int? CursoID,
                                  DateTime? currentFechaInicio, DateTime? fechaInicio,
                                  DateTime? currentFechaFin, DateTime? fechaFin,
                                  bool? currentCreadasOtrosUsuarios, bool? creadasOtrosUsuarios,
                                  bool? currentAutorizacionPendiente, bool? autorizacionPendiente, 
                                  int? page, bool? exportarExcel)
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

            //se agrega la hora 23:59:59 para que en el filtro de fecha final se contample todo el día completo
            DateTime fechaFinFiltro = fechaFin != null ? fechaFin.Value.AddMinutes(1439) : DateTime.MinValue;

            ViewBag.CurrentFechaFin = fechaFin;

            if (creadasOtrosUsuarios != null)
                page = 1;
            else
                creadasOtrosUsuarios = currentCreadasOtrosUsuarios;

            ViewBag.CurrentCreadasOtrosUsuarios = creadasOtrosUsuarios;

            if (autorizacionPendiente != null)
                page = 1;
            else
                autorizacionPendiente = currentAutorizacionPendiente;

            ViewBag.CurrentAutorizacionPendiente = autorizacionPendiente;

            List<Curso> cursosDD = db.Cursos.OrderBy(c => c.Descripcion).ToList();
            cursosDD.Insert(0, new Curso { CursoID = -1, Descripcion = "Todos" });
            ViewBag.CursoID = new SelectList(cursosDD, "CursoID", "Descripcion", CursoID);

            var jornadas = db.Jornada.Include(j => j.Curso).Include(j => j.Instructor).Include(j => j.Lugar);

            if (CursoID != -1)
                jornadas = jornadas.Where(j => j.CursoId == CursoID);

            if (fechaInicio != null)
                jornadas = jornadas.Where(j => j.Fecha >= fechaInicio.Value);

            //el valor indica que no se ingreso un valor correspondiente a la fecha fin para aplicar en el filtro
            if (fechaFinFiltro != DateTime.MinValue)
                jornadas = jornadas.Where(j => j.Fecha <= fechaFinFiltro);

            if (User.IsInRole("InstructorExterno")) //si es un instructor externo, solo se muestran las jornadas asignadas al instructor asociado al usuario actual
            {
                var iu = db.InstructoresUsuarios.Where(ius => ius.Usuario == User.Identity.Name).FirstOrDefault();

                if (iu != null)
                {
                    var instructorUsuario = iu.Instructor;
                    jornadas = jornadas.Where(j => j.Instructor.InstructorID == instructorUsuario.InstructorID);
                }
            }
            else
            {
                bool motrarCreadasOtrosUsuarios = creadasOtrosUsuarios != null ? creadasOtrosUsuarios.Value : true;
                bool mostrarAutorizacionPendiente = autorizacionPendiente != null ? autorizacionPendiente.Value : false;

                //si no se aplicó el filtro de ver las jornadas creadas por otro usuarios (está opción solo es válida para el rol Administrador)
                //o si el usuario tiene el rol AdministradorExterno, solo se mostrarán las jornadas creadas por el usuario
                if (!motrarCreadasOtrosUsuarios || User.IsInRole("AdministradorExterno") || User.IsInRole("InstructorExterno"))
                    jornadas = jornadas.Where(j => j.UsuarioModificacion == User.Identity.Name);

                if (mostrarAutorizacionPendiente)
                    jornadas = jornadas.Where(j => j.RequiereAutorizacion && !j.Autorizada);
            }

            int pageSize = 50;
            int pageNumber = (page ?? 1);

            jornadas = jornadas.OrderByDescending(j => j.Fecha).ThenByDescending(j => j.Hora);

            bool exportar = exportarExcel != null ? (bool)exportarExcel : false;

            if (!exportar)
                return View(jornadas.ToPagedList(pageNumber, pageSize));
            else
                return ExportDataExcelIndex(jornadas.ToList());
        }

        // GET: Jornadas/Details/5
        //[Authorize(Roles = "Administrador,AdministradorExterno,InscripcionesExternas")]
        //este atributo indica al navegador que no se cacheé esta página. De esta forma si se navega a otra página (por ejemplo en la 
        //edición de un capacitado) y se regresa con el "back button", se recarga la página y se ven correctamente los elementos actualizados por JavaScript
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public ActionResult Details(int? id,
                                    bool? exportarExcel,
                                    bool? generarActa,
                                    bool? generarReporteOVAL,
                                    bool? enviarActaEmail)
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
            bool reporteOVAL = generarReporteOVAL != null ? (bool)generarReporteOVAL : false;
            bool enviarActa = enviarActaEmail ?? false;

            if (excel)
                return ExportDataExcel(jornada);

            if (acta)
                return GenerarActa(jornada);

            if (reporteOVAL)
                return GenerarReporteOVAL(jornada);

            if (enviarActa && jornada.Curso.EnviarActaEmail)
            {
                bool emailEnviado = NotificacionesEMailHelper.GetInstance().EnviarEmailJornadaActa(jornada);
                if (emailEnviado)
                    TempData["SuccessMessage"] = "El acta ha sido enviada por correo electrónico.";
                else
                    TempData["ErrorMessage"] = "Hubo un error al enviar el acta por correo electrónico.";
            }

            return View(jornada);

        }

        [Authorize(Roles = "Administrador,InstructorExterno")]
        public ActionResult UltimasCargarFotos(int? page)
        {
            TimeZoneInfo montevideoStandardTime = TimeZoneInfo.FindSystemTimeZoneById("Montevideo Standard Time");
            DateTime dateTime_Montevideo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, montevideoStandardTime);

            //se muestran las jornadas hasta una hora después de iniciadas (si la jornada empieza a 8am, la jornada se muestra en la grilla hasta las 9am)
            DateTime finDelDia = new DateTime(dateTime_Montevideo.Year, dateTime_Montevideo.Month, dateTime_Montevideo.Day, 23, 59, 59);

            int pageSize = 5;
            int pageNumber = (page ?? 1);

            var jornadas = db.Jornada.Where(j => j.Fecha <= finDelDia && j.Autorizada).OrderByDescending(j => j.Fecha).ThenByDescending(j => j.HoraFormatoNumerico).Include(j => j.Curso).Include(j => j.Instructor).Include(j => j.Lugar);

            /*
            //los usuarios con perfil InstructorExterno, solamente pueden ver sus jornadas asignadas
            if (System.Web.HttpContext.Current.User.IsInRole("InstructorExterno"))
            {
                var instructor = UsuarioHelper.GetInstance().ObtenerInstructorAsociado(System.Web.HttpContext.Current.User.Identity.Name);

                ViewBag.InstructorId = instructor.InstructorID;
                ViewBag.InstructorNombreCompleto = instructor.NombreCompleto;
            }
             */

            return View(jornadas.ToPagedList(pageNumber, pageSize));
        }

        // GET: Jornadas/Create - Si se especifica un valor en el parámtero id, se copiaran sus registros de 
        // capacitación a la jornada creada
        public ActionResult Create(int? id)
        {
            ViewBag.CursoId = new SelectList(db.Cursos, "CursoID", "Descripcion");
            ViewBag.LugarID = new SelectList(db.Lugares, "LugarID", "NombreLugar");

            ViewBag.JornadaTemplateId = id;

            //si la jornada está siendo creada por un usuario con perfil para InstructorExterno, solo se puede asignar la jornada a ese instructor
            if (System.Web.HttpContext.Current.User.IsInRole("InstructorExterno"))
            {
                var instructor = UsuarioHelper.GetInstance().ObtenerInstructorAsociado(System.Web.HttpContext.Current.User.Identity.Name);

                ViewBag.InstructorId = instructor.InstructorID;
                ViewBag.InstructorNombreCompleto = instructor.NombreCompleto;
            }
            else
            {
                ViewBag.InstructorId = new SelectList(db.Instructores.Where(i => i.Activo == true), "InstructorID", "NombreCompleto");
            }

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
        public ActionResult Create([Bind(Include = "JornadaID,Fecha,CursoId,LugarID,Direccion,InstructorId,Hora,Caracteristicas,CuposDisponibles,TieneMinimoAsistentes,MinimoAsistentes,TieneMaximoAsistentes,MaximoAsistentes,TieneCierreIncripcion,HorasCierreInscripcion,PermiteInscripcionesExternas,PermiteEnviosOVAL")] Jornada jornada, int? JornadaTemplateId)
        {
            if (ModelState.IsValid)
            {
                jornada.SetearAtributosControl();

                Curso c = db.Cursos.Find(jornada.CursoId);
                jornada.IniciarAtributosAutorizacion(c.RequiereAutorizacion);

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

                        if (jornada.PermiteEnviosOVAL)
                            nuevoRC.EnvioOVALEstado = EstadosEnvioOVAL.PendienteEnvio;
                        else
                            nuevoRC.EnvioOVALEstado = EstadosEnvioOVAL.NoEnviar;

                        db.RegistroCapacitacion.Add(nuevoRC);
                    }
                }

                jornada.HoraFormatoNumerico = int.Parse(jornada.HoraSinSeparador);

                TimeSpan hora = new TimeSpan(jornada.Hora_HH, jornada.Minuto_MM, 0);
                jornada.Fecha = jornada.Fecha.Add(hora);

                db.Jornada.Add(jornada);
                db.SaveChanges();

                //si la jornada fue creada por un usuario con perfil para InstructorExterno, se notifica por email
                if (System.Web.HttpContext.Current.User.IsInRole("InstructorExterno"))
                {
                    //se recarga la jornada para incluir los cursos
                    var jornadaC = db.Jornada.Where(j => j.JornadaID == jornada.JornadaID).Include(j => j.Curso).Include(j => j.Lugar).FirstOrDefault();
                    NotificacionesEMailHelper.GetInstance().EnviarEmailJornadaCreacion(jornadaC);
                }

                if (JornadaTemplateId == null)
                {
                    return RedirectToAction("Index");
                }
                else
                {
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

            if (jornada.PuedeEditarUsuarioActual)
            {
                ViewBag.CursoId = new SelectList(db.Cursos, "CursoID", "Descripcion", jornada.CursoId);
                ViewBag.LugarID = new SelectList(db.Lugares, "LugarID", "NombreLugar", jornada.LugarID);

                //si la jornada está siendo editada por un usuario con perfil para InstructorExterno, solo se puede asignar la jornada a ese instructor
                //también se verifica que la jornada esté asignada a ese instructor
                if (System.Web.HttpContext.Current.User.IsInRole("InstructorExterno"))
                {
                    var instructor = UsuarioHelper.GetInstance().ObtenerInstructorAsociado(System.Web.HttpContext.Current.User.Identity.Name);

                    if (jornada.InstructorId != instructor.InstructorID)
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

                    ViewBag.InstructorId = instructor.InstructorID;
                    ViewBag.InstructorNombreCompleto = instructor.NombreCompleto;
                }
                else
                {
                    ViewBag.InstructorId = new SelectList(db.Instructores.Where(i => i.Activo == true), "InstructorID", "NombreCompleto", jornada.InstructorId);
                }

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
        public ActionResult Edit([Bind(Include = "JornadaID,Fecha,CursoId,LugarID,Direccion,InstructorId,Hora,Caracteristicas,CuposDisponibles,TieneMinimoAsistentes,MinimoAsistentes,TieneMaximoAsistentes,MaximoAsistentes,TieneCierreIncripcion,HorasCierreInscripcion,Autorizada,PermiteInscripcionesExternas,PermiteEnviosOVAL")] Jornada jornada)
        {
            if (ModelState.IsValid)
            {
                jornada.SetearAtributosControl();

                jornada.HoraFormatoNumerico = int.Parse(jornada.HoraSinSeparador);

                TimeSpan hora = new TimeSpan(jornada.Hora_HH, jornada.Minuto_MM, 0);
                jornada.Fecha = jornada.Fecha.Add(hora);

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

        public ActionResult ToggleAutorizada(int? id)
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
                jornada.ToggleAutorizada();
                db.SaveChanges();

                return RedirectToAction("Details", new { id = id });
            }
            else
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
        }

        public ActionResult IngresarCalificaciones(int? id)
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

            return View(jornada);

            /*
            if (jornada.PuedeModificarse())
            {
                return View(jornada);
            }
            else
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            */
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
                FechaVencimiento = jornada.ObtenerFechaVencimiento(),
            };

            if (jornada.PermiteEnviosOVAL)
                registroCapacitacion.EnvioOVALEstado = EstadosEnvioOVAL.PendienteEnvio;
            else
                registroCapacitacion.EnvioOVALEstado = EstadosEnvioOVAL.NoEnviar;

            registroCapacitacion.SetearAtributosControl();
            db.RegistroCapacitacion.Add(registroCapacitacion);

            db.SaveChanges();

            //si la incripción fue registrada por un usuario con perfil para inscripciones externas, se notifica por email
            if (System.Web.HttpContext.Current.User.IsInRole("InscripcionesExternas"))
                NotificacionesEMailHelper.GetInstance().EnviarEmailsNotificacionInscripcionExterna(registroCapacitacion, false);

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

        [HttpGet]
        public ActionResult ObtenerRegistrosCapacitacionFotos(int jornadaId)
        {
            var jornada = db.Jornada.Find(jornadaId);

            if (jornada == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            return PartialView("_ListRegistrosCapacitacionFotosPartial", jornada.RegistrosCapacitacion.ToList());
        }

        [HttpGet]
        public ActionResult ObtenerRegistrosCapacitacionOVAL(int jornadaId)
        {
            var jornada = db.Jornada.Find(jornadaId);

            if (jornada == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            return PartialView("_ListRegistrosCapacitacionOvalPartial", jornada.RegistrosCapacitacion.AsQueryable().ToPagedList(1, jornada.RegistrosCapacitacion.Count()));
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
                    TipoArchivo = Models.Enums.TiposArchivo.ActaEscaneada,
                    FechaArchivo = DateTime.Now
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

        // GET: Jornadas/Details/5
        //[Authorize(Roles = "Administrador,AdministradorExterno,InscripcionesExternas")]
        public ActionResult CargarFotos(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Jornada jornada = db.Jornada.Where(j => j.JornadaID == id).Include(j => j.RegistrosCapacitacion).FirstOrDefault();
            if (jornada == null)
            {
                return HttpNotFound();
            }

            return View(jornada);
        }

        private ActionResult ExportDataExcelIndex(List<Jornada> jornadas)
        {
            
            using (ExcelPackage package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Jornadas");

                const int rowInicial = 1;
                int i = rowInicial + 1;

                ws.Cells[rowInicial, 1].Value = "Curso";
                ws.Cells[rowInicial, 2].Value = "Intructor";
                ws.Cells[rowInicial, 3].Value = "Lugar";
                ws.Cells[rowInicial, 4].Value = "Dirección";
                ws.Cells[rowInicial, 5].Value = "Fecha";
                ws.Cells[rowInicial, 6].Value = "Hora";

                //se ponen en negrita los encabezados
                ws.Cells[rowInicial, 1, rowInicial, 6].Style.Font.Bold = true;

                var bgColor = Color.WhiteSmoke;

                foreach (var j in jornadas)
                {
                    ws.Cells[i, 1].Value = j.Curso.Descripcion;
                    ws.Cells[i, 2].Value = j.Instructor.NombreCompleto;
                    ws.Cells[i, 3].Value = j.Lugar.NombreLugar;
                    ws.Cells[i, 4].Value = j.Direccion;
                    ws.Cells[i, 5].Value = j.Fecha.ToShortDateString();
                    ws.Cells[i, 6].Value = j.Hora;

                    //se seleccionan las columnas con datos del capacitado para setear el background color.
                    if (bgColor != Color.White)
                    {
                        ws.Cells[i, 1, i, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[i, 1, i, 6].Style.Fill.BackgroundColor.SetColor(bgColor);
                    }

                    //se pone un borde alrededor del renglón del encabezado
                    //ws.Cells[i - 1, 1, i - 1, j - 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    bgColor = bgColor == Color.White ? Color.WhiteSmoke : Color.White;

                    i++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = "jornadas.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }

        private ActionResult ExportDataExcel(Jornada j)
        {
            // Llamada a la función del helper para generar el archivo Excel en un MemoryStream
            var stream = ExportarExcelHelper.GetInstance().GenerarJornadaExcelStream(j);

            string fileName = $"{j.JornadaIdentificacionCompleta}.xlsx";
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return File(stream, contentType, fileName);
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
                    wsFormat.Cells[1, colFecha].Value = String.Format("{0}: {1} {2}", "Fecha", j.Fecha.ToShortDateString(), j.Hora);
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

                string fileName = String.Format("ACTA {0} {1} {2}.xlsx", j.Curso.Descripcion, j.FechaFormatoYYYYYMMDD, j.HoraSinSeparador);
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }

        private ActionResult GenerarReporteOVAL(Jornada j)
        {
            const int heightSeparaciónCabezal = 40;

            const int colLabelCapacitacion = 2;
            const int colCurso = 3;

            const int colLabelLugar = 5;
            const int colLugar = 6;

            const int colFecha = 8;

            const int heightDetalleCapacitado = 30;

            const int rowHeaderCapacitados = 3;

            const int colOrdinal = 1;
            const int colNombre = 2;
            const int colApellido = 3;
            const int colTipoDocumento = 4;
            const int colDocumento = 5;
            const int colEmpresa = 6;
            const int colEstadoOVAL = 7;
            const int colMensajeOVAL = 8;
            const int colFechaHoraOVAL = 9;

            const int widthOrdinal = 8;

            using (ExcelPackage package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add(j.Curso.Descripcion);

                int ordinal = 1;
                int rowActual = rowHeaderCapacitados;

                foreach (var r in j.RegistrosCapacitacion)
                {
                    rowActual++;

                    ws.Cells[rowActual, colOrdinal].Value = ordinal;
                    ws.Cells[rowActual, colOrdinal].Style.Font.Bold = true;
                    ws.Cells[rowActual, colOrdinal].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    ws.Cells[rowActual, colNombre].Value = r.Capacitado.Nombre;
                    ws.Cells[rowActual, colApellido].Value = r.Capacitado.Apellido;
                    ws.Cells[rowActual, colTipoDocumento].Value = r.Capacitado.TipoDocumento.Abreviacion;
                    ws.Cells[rowActual, colDocumento].Value = r.Capacitado.Documento;
                    ws.Cells[rowActual, colEmpresa].Value = r.Capacitado.Empresa.NombreFantasia;
                    ws.Cells[rowActual, colEstadoOVAL].Value = r.EnvioOVALEstado;
                    ws.Cells[rowActual, colMensajeOVAL].Value = r.EnvioOVALMensaje;
                    ws.Cells[rowActual, colFechaHoraOVAL].Value = r.EnvioOVALFechaHora.ToString();

                    if (r.EnvioOVALEstado == EstadosEnvioOVAL.Rechazado)
                    {
                        ws.Cells[rowActual, colOrdinal, rowActual, colFechaHoraOVAL].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[rowActual, colOrdinal, rowActual, colFechaHoraOVAL].Style.Fill.BackgroundColor.SetColor(Color.OrangeRed);
                    }

                    ws.Row(rowActual).Height = heightDetalleCapacitado;

                    ordinal++;
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
                    wsFormat.Cells[rowHeaderCapacitados, colEmpresa].Value = "Empresa";
                    wsFormat.Cells[rowHeaderCapacitados, colEstadoOVAL].Value = "Estado último envío";
                    wsFormat.Cells[rowHeaderCapacitados, colMensajeOVAL].Value = "Mensaje último envío";
                    wsFormat.Cells[rowHeaderCapacitados, colFechaHoraOVAL].Value = "Fecha último envío";
                    
                    //se setea el estilo del cabezal de los capacitados
                    var cellsHeaderCapacitados = wsFormat.Cells[rowHeaderCapacitados, colOrdinal, rowHeaderCapacitados, colFechaHoraOVAL];

                    cellsHeaderCapacitados.Style.Font.Bold = true;
                    cellsHeaderCapacitados.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    wsFormat.Row(rowHeaderCapacitados).Height = heightDetalleCapacitado;

                    //Cuerpo y Pie de página ////////////////////////////////////////////////////////////////////////////
                    var rangoContenido = wsFormat.Cells[rowHeaderCapacitados, colOrdinal, rowActual, colFechaHoraOVAL];

                    rangoContenido.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                    rangoContenido.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    rangoContenido.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    rangoContenido.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    rangoContenido.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                    var rangoContenidoTipoDocumento = wsFormat.Cells[rowHeaderCapacitados + 1, colTipoDocumento, rowActual - 1, colTipoDocumento];
                    rangoContenidoTipoDocumento.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    //seteo de anchos de columna
                    wsFormat.Cells[wsFormat.Dimension.Address].AutoFitColumns();
                    wsFormat.Column(colOrdinal).Width = widthOrdinal;

                    //seteo de los parámetros de impresión
                    wsFormat.PrinterSettings.PaperSize = ePaperSize.A4;
                    wsFormat.PrinterSettings.Orientation = eOrientation.Portrait;
                    //wsFormat.PrinterSettings.FitToPage = true;

                    paginaActual++;
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = String.Format("Envíos OVAL - {0} {1}.xlsx", j.Curso.Descripcion, j.FechaFormatoYYYYYMMDD);
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

        public ActionResult ObtenerDatosDisponibilidadCupos(int JornadaId)
        {
            var jornada = db.Jornada.Where(j => j.JornadaID == JornadaId).FirstOrDefault();

            if (jornada != null)
            {
                var datosDisponibilidadCupos = new
                {
                    QuedanCuposDisponibles = jornada.QuedanCuposDisponibles,
                    TotalInscriptos = jornada.TotalInscriptos,
                    LabelTotalCuposDisponibles = JornadaHelper.GetInstance().ObtenerLabelTotalCuposDisponibles(jornada),
                    LabelTotalInscriptos = JornadaHelper.GetInstance().ObtenerLabelTotalInscriptos(jornada)
                };

                return Json(datosDisponibilidadCupos, JsonRequestBehavior.AllowGet);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerDatosDocumentacionAdicional(int JornadaId)
        {
            var jornada = db.Jornada.Where(j => j.JornadaID == JornadaId).FirstOrDefault();

            if (jornada != null)
            {
                var datosDocumentacionAdicional = new
                {
                    DocumentacionAdicionalCompleta = jornada.DocumentacionAdicionalCompleta,
                    LabelDocumentacionAdicional = JornadaHelper.GetInstance().ObtenerLabelDocumentacionAdicional(jornada)
                };

                return Json(datosDocumentacionAdicional, JsonRequestBehavior.AllowGet);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        public ActionResult EnviarDatosOVAL(int jornadaId)
        {
            var jornada = db.Jornada.Where(j => j.JornadaID == jornadaId).FirstOrDefault();

            if (jornada != null)
            {
                int totalAceptados = 0;
                int totalRechazados = 0;

                bool todosEnviadosOK = EnvioOVALHelper.GetInstance().EnviarDatosListaRegistros(jornada.RegistrosCapacitacion.Select(r => r.RegistroCapacitacionID).ToList(), ref totalAceptados, ref totalRechazados);

                var resultadoEnviarDatosOVAL = new
                {
                    todosEnviadosOK = todosEnviadosOK,
                    totalAceptados = totalAceptados,
                    totalRechazados = totalRechazados
                };

                return Json(resultadoEnviarDatosOVAL, JsonRequestBehavior.AllowGet);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        public ActionResult Disponibles()
        {
            TimeZoneInfo montevideoStandardTime = TimeZoneInfo.FindSystemTimeZoneById("Montevideo Standard Time");
            DateTime dateTime_Montevideo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, montevideoStandardTime);
            dateTime_Montevideo = dateTime_Montevideo.AddHours(-1);

            var jornadas = db.Jornada.Where(j => j.Fecha >= dateTime_Montevideo && j.Autorizada)
                                      .OrderBy(j => j.Fecha)
                                      .ThenBy(j => j.HoraFormatoNumerico)
                                      .Include(j => j.Curso)
                                      .Include(j => j.Instructor)
                                      .Include(j => j.Lugar)
                                      .ToList();

            // Se consultan los mensajes específicos para esta vista filtrando por IdentificadorInterno
            var mensaje1 = db.MensajesUsuarios.FirstOrDefault(m => m.IdentificadorInterno == "DisponiblesMensaje1");
            var mensaje2 = db.MensajesUsuarios.FirstOrDefault(m => m.IdentificadorInterno == "DisponiblesMensaje2");

            var viewModel = new JornadasDisponiblesViewModel
            {
                Jornadas = jornadas,
                MensajeDisponibles1 = mensaje1,
                MensajeDisponibles2 = mensaje2
            };

            return View(viewModel);
        }
    }
}