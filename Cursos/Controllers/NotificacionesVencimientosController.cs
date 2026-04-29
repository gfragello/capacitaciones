using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Cursos.Models;
using Cursos.Helpers;
using Cursos.Models.Enums;
using System.Net.Mail;
using System.Threading.Tasks;
using OfficeOpenXml;
using System.IO;

namespace Cursos.Controllers
{
    public class NotificacionesVencimientosController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: NotificacionesVencimientos
        public ActionResult Index(bool? reporteVencimientos)
        {
            bool reporte = reporteVencimientos != null ? (bool)reporteVencimientos : false;

            if (!reporte)
                return View(ObtenerNotificacionesVencimiento(null));
            else
                return GenerarReporteVencimientos();
        }

        // GET: NotificacionesVencimientos/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NotificacionVencimiento notificacionVencimiento = db.NotificacionVencimientos.Find(id);
            if (notificacionVencimiento == null)
            {
                return HttpNotFound();
            }
            return View(notificacionVencimiento);
        }

        // GET: NotificacionesVencimientos/Create
        public ActionResult Create()
        {
            ViewBag.RegistroCapacitacionID = new SelectList(db.RegistroCapacitacion, "RegistroCapacitacionID", "UsuarioModificacion");
            return View();
        }

        // POST: NotificacionesVencimientos/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "NotificacionVencimientoID,MailNotificacionVencimiento,RegistroCapacitacionID")] NotificacionVencimiento notificacionVencimiento)
        {
            if (ModelState.IsValid)
            {
                db.NotificacionVencimientos.Add(notificacionVencimiento);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.RegistroCapacitacionID = new SelectList(db.RegistroCapacitacion, "RegistroCapacitacionID", "UsuarioModificacion", notificacionVencimiento.RegistroCapacitacionID);
            return View(notificacionVencimiento);
        }

        // GET: NotificacionesVencimientos/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NotificacionVencimiento notificacionVencimiento = db.NotificacionVencimientos.Find(id);
            if (notificacionVencimiento == null)
            {
                return HttpNotFound();
            }
            ViewBag.RegistroCapacitacionID = new SelectList(db.RegistroCapacitacion, "RegistroCapacitacionID", "UsuarioModificacion", notificacionVencimiento.RegistroCapacitacionID);
            return View(notificacionVencimiento);
        }

        // POST: NotificacionesVencimientos/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "NotificacionVencimientoID,MailNotificacionVencimiento,RegistroCapacitacionID")] NotificacionVencimiento notificacionVencimiento)
        {
            if (ModelState.IsValid)
            {
                db.Entry(notificacionVencimiento).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.RegistroCapacitacionID = new SelectList(db.RegistroCapacitacion, "RegistroCapacitacionID", "UsuarioModificacion", notificacionVencimiento.RegistroCapacitacionID);
            return View(notificacionVencimiento);
        }

        // GET: NotificacionesVencimientos/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NotificacionVencimiento notificacionVencimiento = db.NotificacionVencimientos.Find(id);
            if (notificacionVencimiento == null)
            {
                return HttpNotFound();
            }
            return View(notificacionVencimiento);
        }

        // POST: NotificacionesVencimientos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            NotificacionVencimiento notificacionVencimiento = db.NotificacionVencimientos.Find(id);
            db.NotificacionVencimientos.Remove(notificacionVencimiento);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult EmpresasIDNotificacionesPendientes()
        {
            return Json(ObtenerEmpresasIDNotificacionesPendientes(), JsonRequestBehavior.AllowGet);
        }

        public List<int> ObtenerEmpresasIDNotificacionesPendientes()
        {
            List<int> empresasID = new List<int>();

            foreach (var notificacionesEmpresa in ObtenerNotificacionesVencimiento(null).GroupBy(n => n.RegistroCapacitacion.Capacitado.EmpresaID))
            {
                empresasID.Add(notificacionesEmpresa.First().RegistroCapacitacion.Capacitado.EmpresaID);
            }

            return empresasID;
        }

        public ActionResult EnviarNotificacionesEmailEmpresa(int empresaId)
        {
            return Json(EnviarEmailsEmpresa(empresaId), JsonRequestBehavior.AllowGet);
        }

        public bool EnviarEmailsEmpresa(int empresaId)
        {
            var empresa = db.Empresas.Where(e => e.EmpresaID == empresaId).FirstOrDefault();

            if (!string.IsNullOrEmpty(empresa.Email)) //si la empresa tiene direcciones de email asociadas
            {
                var message = new MailMessage();

                foreach (var emailEmpresa in empresa.Email.Split(','))
                {
                    message.To.Add(new MailAddress(emailEmpresa));
                }

                foreach (var emailCC in ConfiguracionHelper.GetInstance().GetValue("EmailNotificacionCC", "Notificaciones").Split(','))
                {
                    message.CC.Add(new MailAddress(emailCC));
                }
            
                message.From = new MailAddress("notificaciones@csl.uy");  // replace with valid value
                message.Subject = string.Format("{0} - {1} {2} {3}",
                                                ConfiguracionHelper.GetInstance().GetValue("EmailNotificacionAsunto", "Notificaciones"),
                                                empresa.NombreFantasia,
                                                DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString());
                message.Body = ConfiguracionHelper.GetInstance().GetValue("EmailNotificacionCuerpo", "Notificaciones");
                message.IsBodyHtml = true;

                message.Body += AgregarTableCapacitados(ObtenerNotificacionesVencimiento(empresaId));

                using (var smtp = new SmtpClient())
                {
                    var credential = new NetworkCredential
                    {
                        UserName = ConfiguracionHelper.GetInstance().GetValue("EmailUsuario", "Notificaciones"),
                        Password = ConfiguracionHelper.GetInstance().GetValue("PasswordUsuario", "Notificaciones")
                        //UserName = "notificaciones@csl.uy",
                        //Password = "n0tiFic@c1on3s"
                    };

                    smtp.Credentials = credential;
                    smtp.Host = ConfiguracionHelper.GetInstance().GetValue("SMPTHost", "Notificaciones");
                    smtp.Port = int.Parse(ConfiguracionHelper.GetInstance().GetValue("SMTPPort", "Notificaciones"));
                    smtp.EnableSsl = bool.Parse(ConfiguracionHelper.GetInstance().GetValue("SMTPSSL", "Notificaciones"));

                    try
                    {
                        smtp.Send(message);
                        db.SaveChanges(); //se modificó el estado de las notificaciones a "Enviada"

                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
        }

        private string AgregarTableCapacitados(List<NotificacionVencimiento> notificaciones)
        {
            string html = IniciarTableCapacitados();

            string backgroundColor = string.Empty;

            foreach (var n in notificaciones)
            {
                if (backgroundColor != "white")
                    backgroundColor = "white";
                else
                    backgroundColor = "#f9f9f9";

                html += AgregarATableCapacitados(n, backgroundColor);
                ActualizarNotificacionEnviada(n.NotificacionVencimientoID);
            }

            html += CerrarTableCapacitados();

            return html;
        }

        private void ActualizarNotificacionEnviada(int notificacionVencimientoId)
        {
            var notificacion = db.NotificacionVencimientos.Local
                .FirstOrDefault(n => n.NotificacionVencimientoID == notificacionVencimientoId);

            if (notificacion == null)
            {
                notificacion = new NotificacionVencimiento
                {
                    NotificacionVencimientoID = notificacionVencimientoId
                };

                db.NotificacionVencimientos.Attach(notificacion);
            }

            notificacion.Estado = EstadoNotificacionVencimiento.Notificado;
            notificacion.Fecha = DateTime.Now;
        }

        private string IniciarTableCapacitados()
        {
            string html = string.Empty;

            html += "<table>";
            html += "<tr style='background-color: #f9f9f9;'>";
            html += "<th style='width: 50%; text-align:left'>";
            html += "Nombre";
            html += "</th>";
            html += "<th style='width: 20%; text-align:left'>";
            html += "Documento";
            html += "</th>";
            html += "<th style='width: 20%; text-align:left'>";
            html += "Curso";
            html += "</th>";
            html += "<th style='width: 10%; text-align:left'>";
            html += "Vencimiento";
            html += "</th>";
            html += "</tr>";

            return html;
        }

        private string AgregarATableCapacitados(NotificacionVencimiento n, string backgroundColor)
        {
            string html = string.Empty;

            html += string.Format("<tr style='background-color: {0}'>", backgroundColor);
            html += "<td>";
            html += n.RegistroCapacitacion.Capacitado.NombreCompleto;
            html += "</td>";
            html += "<td>";
            html += n.RegistroCapacitacion.Capacitado.DocumentoCompleto;
            html += "</td>";
            html += "<td>";
            html += n.RegistroCapacitacion.Jornada.Curso.Descripcion;
            html += "</td>";
            html += "<td>";
        html += n.RegistroCapacitacion.FechaVencimiento.HasValue ?
            n.RegistroCapacitacion.FechaVencimiento.Value.ToShortDateString() :
            "Sin vencimiento";
            html += "</td>";
            html += "</tr> ";

            return html;
        }

        private string CerrarTableCapacitados()
        {
            return "<table>";
        }

        private List<NotificacionVencimiento> ObtenerNotificacionesVencimiento(int? empresaId)
        {
            int antelacionNotificacion = int.Parse(ConfiguracionHelper.GetInstance().GetValue("AntelacionNotificacion", "Notificaciones"));

            DateTime proximaFechaVencimientoNotificar = DateTime.Now.AddDays(antelacionNotificacion);

            // Obtener notificaciones pendientes solo de cursos que tienen habilitada la notificación de vencimiento
            var notificacionVencimientos = db.NotificacionVencimientos
                                             .AsNoTracking()
                                             .Include(n => n.RegistroCapacitacion)
                                             .Include(n => n.RegistroCapacitacion.Capacitado)
                                             .Include(n => n.RegistroCapacitacion.Capacitado.Empresa)
                                             .Include(n => n.RegistroCapacitacion.Jornada)
                                             .Include(n => n.RegistroCapacitacion.Jornada.Curso)
                                             .Where(n => n.RegistroCapacitacion.Jornada.Curso.NotificarVencimiento)
                                             .Where(n => n.Estado == EstadoNotificacionVencimiento.NotificacionPendiente)
                                             .Where(n => n.RegistroCapacitacion.FechaVencimiento.HasValue && n.RegistroCapacitacion.FechaVencimiento.Value <= proximaFechaVencimientoNotificar);

            if (empresaId != null)
                notificacionVencimientos = notificacionVencimientos.Where(n => n.RegistroCapacitacion.Capacitado.EmpresaID == empresaId);

            var notificacionVencimientosOrdenadas = notificacionVencimientos
                                                    .OrderBy(n => n.RegistroCapacitacion.Capacitado.Empresa.NombreFantasia)
                                                    .ThenBy(n => n.RegistroCapacitacion.Jornada.Curso.CursoID)
                                                    .ThenBy(n => n.RegistroCapacitacion.Capacitado.Apellido)
                                                    .ThenBy(n => n.RegistroCapacitacion.Capacitado.Nombre);

            var notificacionVencimientosRet = notificacionVencimientosOrdenadas.ToList();

            //int totalRegistros = notificacionVencimientos.Count();
            bool generarArchivoIds = bool.Parse(ConfiguracionHelper.GetInstance().GetValue("GenerarArchivoIds", "Notificaciones"));

            if (generarArchivoIds)
            {
                LogHelper.GetInstance().WriteMessage("notificaciones", "------------------------------------");

                //se escriben los ids de las notificaciones
                LogHelper.GetInstance().WriteMessage("notificaciones", "NotificacionVencimientoID:");
                foreach (NotificacionVencimiento n in notificacionVencimientosRet)
                {
                    LogHelper.GetInstance().WriteMessage("notificaciones", n.NotificacionVencimientoID.ToString());
                }

                //se escriben los ids de los registros de capacitación
                LogHelper.GetInstance().WriteMessage("notificaciones", "RegistroCapacitacionID:");
                foreach (NotificacionVencimiento n in notificacionVencimientosRet)
                {
                    LogHelper.GetInstance().WriteMessage("notificaciones", n.RegistroCapacitacionID.ToString());
                }

                LogHelper.GetInstance().WriteMessage("notificaciones", "------------------------------------");
            }

            notificacionVencimientosRet.RemoveAll(n => n.Estado == EstadoNotificacionVencimiento.NoNotificarYaActualizado);

            return notificacionVencimientosRet;
        }

        //Las notificaciones asociadas a los registros se crean automáticamente al aprobar el registro
        //Este método mantiene compatibilidad con registros antiguos (legacy) que no tienen notificaciones
        //y ejecuta limpieza preventiva de notificaciones obsoletas
        private void ActualizarNotificacionesVencimientos()
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                // Buscar registros aprobados sin notificación asociada (registros legacy)
                var rcSinNotificacioneAsociadas =
                db.RegistroCapacitacion
                    .Include(r => r.Jornada)
                    .Include(r => r.Jornada.Curso)
                    .Include(r => r.Capacitado)
                    .Where(r => r.FechaVencimiento.HasValue && 
                                !db.NotificacionVencimientos.Any(n => n.RegistroCapacitacionID == r.RegistroCapacitacionID) &&
                                (r.Estado == EstadosRegistroCapacitacion.Aprobado || r.Estado == EstadosRegistroCapacitacion.NoAprobado))
                    .ToList();

                if (rcSinNotificacioneAsociadas.Count > 0)
                {
                    LogHelper.GetInstance().WriteMessage("notificaciones", 
                        $"Se encontraron {rcSinNotificacioneAsociadas.Count} registros legacy sin notificación asociada");

                    foreach (var r in rcSinNotificacioneAsociadas)
                    {
                        // Solo crear notificación si el curso tiene habilitada la notificación de vencimiento
                        if (!r.Jornada.Curso.NotificarVencimiento)
                        {
                            LogHelper.GetInstance().WriteMessage("notificaciones", 
                                $"Registro {r.RegistroCapacitacionID}: El curso {r.Jornada.Curso.Descripcion} no tiene notificación de vencimiento habilitada");
                            continue;
                        }

                        EstadoNotificacionVencimiento estadoNotificacion =
                        VerificarCursoYaActualizado(r) ? EstadoNotificacionVencimiento.NoNotificarYaActualizado : EstadoNotificacionVencimiento.NotificacionPendiente;

                        if (estadoNotificacion == EstadoNotificacionVencimiento.NoNotificarYaActualizado)
                        {
                            Capacitado c = r.Capacitado;
                            Jornada jv = r.Jornada; //jornada vencida

                            string mensajelog =
                            string.Format("{0} {1} - {2}. No se notificará el vencimiento de la jornada {3} porque el Capacitado ya tiene una jornada posterior correspondiente a ese curso.",
                            r.RegistroCapacitacionID,
                            c.DocumentoCompleto,
                            c.NombreCompleto,
                            jv.JornadaIdentificacionCompleta);

                            LogHelper.GetInstance().WriteMessage("notificaciones", mensajelog);
                        }

                        var n = new NotificacionVencimiento
                        {
                            Estado = estadoNotificacion,
                            RegistroCapacitacion = r,
                            Fecha = DateTime.Now,
                            MailNotificacionVencimiento = r.Capacitado.Empresa?.Email
                        };

                        db.NotificacionVencimientos.Add(n);
                    }

                    db.SaveChanges();
                    
                    LogHelper.GetInstance().WriteMessage("notificaciones", 
                        $"Se crearon notificaciones legacy para {rcSinNotificacioneAsociadas.Count} registros");
                }
            }

            // Ejecutar limpieza preventiva de notificaciones obsoletas
            // Esto cubre casos donde un capacitado aprobó un curso posterior después de que la notificación fue creada
            try
            {
                int notificacionesActualizadas = LimpiarNotificacionesObsoletas();
                
                if (notificacionesActualizadas > 0)
                {
                    LogHelper.GetInstance().WriteMessage("notificaciones", 
                        $"Limpieza preventiva: {notificacionesActualizadas} notificaciones marcadas como NoNotificarYaActualizado");
                }
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance().WriteMessage("notificaciones", 
                    $"Error en limpieza preventiva de notificaciones: {ex.Message}");
            }
        }

        public ActionResult MarcarNotificacionesPendientesEmpresaNoNotificar(int EmpresaID)
        {
            var notificacionesPendientesEmpresa = db.NotificacionVencimientos
                                                    .Where(n => n.Estado == EstadoNotificacionVencimiento.NotificacionPendiente &&
                                                                n.RegistroCapacitacion.Capacitado.EmpresaID == EmpresaID).ToList();

            if (notificacionesPendientesEmpresa.Count > 0)
            {
                foreach (var n in notificacionesPendientesEmpresa)
                {
                    n.Estado = EstadoNotificacionVencimiento.NoNotificar;
                }

                db.SaveChanges();
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Crea una notificación de vencimiento para un registro de capacitación recién aprobado.
        /// Este método se invoca automáticamente al aprobar un registro.
        /// </summary>
        /// <param name="registroCapacitacionId">ID del registro de capacitación aprobado</param>
        /// <returns>True si se creó la notificación, False si no se debe notificar</returns>
        public bool CrearNotificacionVencimiento(int registroCapacitacionId)
        {
            using (ApplicationDbContext dbContext = new ApplicationDbContext())
            {
                var registro = dbContext.RegistroCapacitacion
                    .Include(r => r.Jornada)
                    .Include(r => r.Jornada.Curso)
                    .Include(r => r.Capacitado)
                    .Include(r => r.Capacitado.Empresa)
                    .FirstOrDefault(r => r.RegistroCapacitacionID == registroCapacitacionId);

                if (registro == null)
                {
                    LogHelper.GetInstance().WriteMessage("notificaciones", 
                        $"Error: No se encontró el registro de capacitación {registroCapacitacionId}");
                    return false;
                }

                // Verificar si el curso tiene notificación de vencimiento habilitada
                if (!registro.Jornada.Curso.NotificarVencimiento)
                {
                    LogHelper.GetInstance().WriteMessage("notificaciones", 
                        $"Registro {registroCapacitacionId}: El curso {registro.Jornada.Curso.Descripcion} no tiene notificación de vencimiento habilitada");
                    return false;
                }

                // Verificar si el registro tiene fecha de vencimiento
                if (!registro.FechaVencimiento.HasValue)
                {
                    LogHelper.GetInstance().WriteMessage("notificaciones", 
                        $"Registro {registroCapacitacionId}: No tiene fecha de vencimiento");
                    return false;
                }

                // Verificar si ya existe una notificación para este registro
                var notificacionExistente = dbContext.NotificacionVencimientos
                    .FirstOrDefault(n => n.RegistroCapacitacionID == registroCapacitacionId);

                if (notificacionExistente != null)
                {
                    LogHelper.GetInstance().WriteMessage("notificaciones", 
                        $"Registro {registroCapacitacionId}: Ya existe una notificación (ID: {notificacionExistente.NotificacionVencimientoID})");
                    return false;
                }

                // Determinar el estado de la notificación
                // Verificar si el capacitado ya tiene un registro posterior aprobado del mismo curso
                EstadoNotificacionVencimiento estadoNotificacion = 
                    VerificarCursoYaActualizado(registro) 
                        ? EstadoNotificacionVencimiento.NoNotificarYaActualizado 
                        : EstadoNotificacionVencimiento.NotificacionPendiente;

                // Crear la notificación
                var notificacion = new NotificacionVencimiento
                {
                    Estado = estadoNotificacion,
                    RegistroCapacitacionID = registroCapacitacionId,
                    Fecha = DateTime.Now,
                    MailNotificacionVencimiento = registro.Capacitado.Empresa?.Email
                };

                dbContext.NotificacionVencimientos.Add(notificacion);
                dbContext.SaveChanges();

                LogHelper.GetInstance().WriteMessage("notificaciones", 
                    $"Registro {registroCapacitacionId}: Notificación creada exitosamente con estado {estadoNotificacion}. " +
                    $"Capacitado: {registro.Capacitado.NombreCompleto}, Curso: {registro.Jornada.Curso.Descripcion}, " +
                    $"Vencimiento: {registro.FechaVencimiento.Value.ToShortDateString()}");

                return true;
            }
        }

        private bool VerificarCursoYaActualizado(RegistroCapacitacion registroAnalizado)
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                Capacitado c = db.Capacitados.Where(cap => cap.CapacitadoID == registroAnalizado.CapacitadoID).Include(cap => cap.RegistrosCapacitacion).FirstOrDefault(); //Find(registroAnalizado.CapacitadoID);

                //para ese capacitado se buscan los registros las jornadas de cursos iguales que se hayan realizado en fechas posteriores
                var registrosPosteriores =
                c.RegistrosCapacitacion.Where(r => r.Jornada.CursoId == registroAnalizado.Jornada.CursoId &&
                                                                        r.Jornada.Fecha > registroAnalizado.Jornada.Fecha);

                return registrosPosteriores.Count() > 0;
            }
        }

        /// <summary>
        /// Actualiza las notificaciones de vencimiento pendientes de un capacitado para un curso específico,
        /// marcándolas como NoNotificarYaActualizado cuando existen registros posteriores aprobados del mismo curso.
        /// Este método se invoca automáticamente al aprobar un registro de capacitación.
        /// </summary>
        /// <param name="capacitadoId">ID del capacitado</param>
        /// <param name="cursoId">ID del curso</param>
        /// <param name="fechaJornadaAprobada">Fecha de la jornada recién aprobada</param>
        /// <returns>Cantidad de notificaciones actualizadas</returns>
        public int ActualizarNotificacionesObsoletasPorCapacitado(int capacitadoId, int cursoId, DateTime fechaJornadaAprobada)
        {
            using (ApplicationDbContext dbContext = new ApplicationDbContext())
            {
                // Excluir cursos específicos según la lógica del SQL original (CursoID 2, 4, 5)
                var cursosExcluidos = new[] { 2, 4, 5 };
                
                if (cursosExcluidos.Contains(cursoId))
                    return 0;

                // Buscar notificaciones pendientes del capacitado para el mismo curso
                // donde la jornada asociada sea anterior a la que se acaba de aprobar
                var notificacionesObsoletas = dbContext.NotificacionVencimientos
                    .Where(nv => nv.Estado == EstadoNotificacionVencimiento.NotificacionPendiente)
                    .Where(nv => nv.RegistroCapacitacion.CapacitadoID == capacitadoId)
                    .Where(nv => nv.RegistroCapacitacion.Jornada.CursoId == cursoId)
                    .Where(nv => nv.RegistroCapacitacion.Jornada.Fecha < fechaJornadaAprobada)
                    .Where(nv => nv.RegistroCapacitacion.FechaVencimiento.HasValue)
                    .ToList();

                // Actualizar el estado de las notificaciones encontradas
                foreach (var notificacion in notificacionesObsoletas)
                {
                    notificacion.Estado = EstadoNotificacionVencimiento.NoNotificarYaActualizado;
                }

                if (notificacionesObsoletas.Any())
                {
                    dbContext.SaveChanges();
                }

                return notificacionesObsoletas.Count;
            }
        }

        /// <summary>
        /// Ejecuta una limpieza preventiva de notificaciones obsoletas.
        /// Marca como NoNotificarYaActualizado aquellas notificaciones pendientes donde el capacitado
        /// ya cursó una jornada posterior del mismo curso.
        /// Este método se ejecuta como respaldo al cargar la vista de notificaciones.
        /// </summary>
        /// <returns>Cantidad de notificaciones actualizadas</returns>
        private int LimpiarNotificacionesObsoletas()
        {
            using (ApplicationDbContext dbContext = new ApplicationDbContext())
            {
                // Ejecutar el UPDATE directamente en SQL para evitar N+1 queries
                // Marca como NoNotificarYaActualizado las notificaciones de cursos que ya fueron actualizados
                string sql = @"
                    UPDATE nv
                    SET nv.Estado = 3  -- NoNotificarYaActualizado
                    FROM dbo.NotificacionesVencimientos nv
                    INNER JOIN dbo.RegistrosCapacitaciones rc ON nv.RegistroCapacitacionID = rc.RegistroCapacitacionID
                    INNER JOIN dbo.Jornadas j ON rc.JornadaID = j.JornadaID
                    INNER JOIN dbo.Cursos c ON j.CursoId = c.CursoID
                    WHERE nv.Estado = 0  -- NotificacionPendiente
                      AND rc.FechaVencimiento IS NOT NULL
                      AND c.NotificarVencimiento = 1  -- Solo cursos con notificación habilitada
                      AND EXISTS (
                        SELECT 1
                        FROM dbo.RegistrosCapacitaciones rc2
                        INNER JOIN dbo.Jornadas j2 ON rc2.JornadaID = j2.JornadaID
                        WHERE rc2.CapacitadoID = rc.CapacitadoID
                          AND j2.CursoId = j.CursoId
                          AND rc2.RegistroCapacitacionID <> rc.RegistroCapacitacionID
                          AND j2.Fecha > j.Fecha
                      )";

                // Ejecutar el comando y obtener el número de filas afectadas
                int filasAfectadas = dbContext.Database.ExecuteSqlCommand(sql);

                return filasAfectadas;
            }
        }

        public ActionResult GenerarReporteVencimientos()
        {
            DateTime fechaDesde = DateTime.Now.AddDays(-30);

            var resultadoTemporal = (from rc in db.RegistroCapacitacion
                                     join c in db.Capacitados on rc.CapacitadoID equals c.CapacitadoID
                                     join nv in db.NotificacionVencimientos on rc.RegistroCapacitacionID equals nv.RegistroCapacitacionID
                                     join j in db.Jornada on rc.JornadaID equals j.JornadaID
                                     join cu in db.Cursos on j.CursoId equals cu.CursoID
                                     join e in db.Empresas on c.EmpresaID equals e.EmpresaID
                                     where rc.FechaVencimiento.HasValue && rc.FechaVencimiento.Value > fechaDesde && cu.NotificarVencimiento
                                     orderby j.Fecha
                                     select new
                                     {
                                         c.Documento,
                                         c.Nombre,
                                         c.Apellido,
                                         Empresa = e.NombreFantasia,
                                         Curso = cu.Descripcion,
                                         Cursado = j.Fecha,
                                         Vencimiento = rc.FechaVencimiento,
                                         EstadoNotificacion = nv.Estado,
                                         Actualizacion = (from j2 in db.Jornada
                                                          join rc2 in db.RegistroCapacitacion on j2.JornadaID equals rc2.JornadaID
                                                          where rc2.CapacitadoID == c.CapacitadoID && j2.Fecha > j.Fecha && j2.CursoId == j.CursoId
                                                          orderby j2.Fecha
                                                          select j2.Fecha).FirstOrDefault(),
                                         rc.RegistroCapacitacionID,
                                         nv.NotificacionVencimientoID
                                     }).ToList();

            var resultado = resultadoTemporal.Select(r => new
            {
                r.Documento,
                r.Nombre,
                r.Apellido,
                r.Empresa,
                r.Curso,
                Cursado = r.Cursado.ToString("dd/MM/yyyy"),
                Vencimiento = r.Vencimiento.HasValue ? r.Vencimiento.Value.ToString("dd/MM/yyyy") : "Sin vencimiento",
                r.EstadoNotificacion,
                Actualizacion = r.Actualizacion.ToString("dd/MM/yyyy") != "01/01/0001" ? r.Actualizacion.ToString("dd/MM/yyyy") : "",
                r.RegistroCapacitacionID,
                r.NotificacionVencimientoID
            }).ToList();

            const int COL_DOCUMENTO = 1;
            const int COL_NOMBRE = 2;
            const int COL_APELLIDO = 3;
            const int COL_EMPRESA = 4;
            const int COL_CURSO = 5;
            const int COL_CURSADO = 6;
            const int COL_VENCIMIENTO = 7;
            const int COL_ESTADO_NOTIFICACION = 8;
            const int COL_ACTUALIZACION = 9;
            const int COL_REGISTRO_CAPACITACION_ID = 10;
            const int COL_NOTIFICACION_VENCIMIENTO_ID = 11;

            using (ExcelPackage excel = new ExcelPackage())
            {
                // Agregando una hoja de trabajo
                var workSheet = excel.Workbook.Worksheets.Add("Resultados");

                // Encabezados
                workSheet.Cells[1, COL_DOCUMENTO].Value = "Documento";
                workSheet.Cells[1, COL_NOMBRE].Value = "Nombre";
                workSheet.Cells[1, COL_APELLIDO].Value = "Apellido";
                workSheet.Cells[1, COL_EMPRESA].Value = "Empresa";
                workSheet.Cells[1, COL_CURSO].Value = "Curso";
                workSheet.Cells[1, COL_CURSADO].Value = "Cursado";
                workSheet.Cells[1, COL_VENCIMIENTO].Value = "Vencimiento";
                workSheet.Cells[1, COL_ESTADO_NOTIFICACION].Value = "Estado Notificación";
                workSheet.Cells[1, COL_ACTUALIZACION].Value = "Actualización";
                workSheet.Cells[1, COL_REGISTRO_CAPACITACION_ID].Value = "Registro Capacitación ID";
                workSheet.Cells[1, COL_NOTIFICACION_VENCIMIENTO_ID].Value = "Notificación Vencimiento ID";

                int row = 2;

                foreach (var item in resultado)
                {
                    workSheet.Cells[row, COL_DOCUMENTO].Value = item.Documento;
                    workSheet.Cells[row, COL_NOMBRE].Value = item.Nombre;
                    workSheet.Cells[row, COL_APELLIDO].Value = item.Apellido;
                    workSheet.Cells[row, COL_EMPRESA].Value = item.Empresa;
                    workSheet.Cells[row, COL_CURSO].Value = item.Curso;
                    workSheet.Cells[row, COL_CURSADO].Value = item.Cursado;
                    workSheet.Cells[row, COL_VENCIMIENTO].Value = item.Vencimiento;
                    workSheet.Cells[row, COL_ESTADO_NOTIFICACION].Value = item.EstadoNotificacion;
                    workSheet.Cells[row, COL_ACTUALIZACION].Value = item.Actualizacion;
                    workSheet.Cells[row, COL_REGISTRO_CAPACITACION_ID].Value = item.RegistroCapacitacionID;
                    workSheet.Cells[row, COL_NOTIFICACION_VENCIMIENTO_ID].Value = item.NotificacionVencimientoID;

                    row++;
                }

                workSheet.Cells[workSheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                excel.SaveAs(stream);

                string fileName = "vencimientos.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
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

        // GET: NotificacionesVencimientos/ObtenerConfiguracionEmail
        public ActionResult ObtenerConfiguracionEmail()
        {
            var asunto = db.Configuracion.Where(c => c.Index == "EmailNotificacionAsunto" && c.Seccion == "Notificaciones").FirstOrDefault();
            var cuerpo = db.Configuracion.Where(c => c.Index == "EmailNotificacionCuerpo" && c.Seccion == "Notificaciones").FirstOrDefault();

            var resultado = new
            {
                Asunto = asunto?.Value ?? "",
                AsuntoId = asunto?.ConfiguracionID ?? 0,
                Cuerpo = cuerpo?.Value ?? "",
                CuerpoId = cuerpo?.ConfiguracionID ?? 0
            };

            return Json(resultado, JsonRequestBehavior.AllowGet);
        }

        // POST: NotificacionesVencimientos/GuardarConfiguracionEmail
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult GuardarConfiguracionEmail(string asunto, string cuerpo)
        {
            try
            {
                var asuntoConfig = db.Configuracion.Where(c => c.Index == "EmailNotificacionAsunto" && c.Seccion == "Notificaciones").FirstOrDefault();
                var cuerpoConfig = db.Configuracion.Where(c => c.Index == "EmailNotificacionCuerpo" && c.Seccion == "Notificaciones").FirstOrDefault();

                if (asuntoConfig != null)
                {
                    asuntoConfig.Value = asunto;
                    db.Entry(asuntoConfig).State = EntityState.Modified;
                }
                else
                {
                    asuntoConfig = new Configuracion
                    {
                        Index = "EmailNotificacionAsunto",
                        Seccion = "Notificaciones",
                        Value = asunto,
                        Label = "Asunto del Email de Notificación",
                        Order = 1
                    };
                    db.Configuracion.Add(asuntoConfig);
                }

                if (cuerpoConfig != null)
                {
                    cuerpoConfig.Value = cuerpo;
                    db.Entry(cuerpoConfig).State = EntityState.Modified;
                }
                else
                {
                    cuerpoConfig = new Configuracion
                    {
                        Index = "EmailNotificacionCuerpo",
                        Seccion = "Notificaciones",
                        Value = cuerpo,
                        Label = "Cuerpo del Email de Notificación",
                        Order = 2
                    };
                    db.Configuracion.Add(cuerpoConfig);
                }

                db.SaveChanges();

                return Json(new { success = true, message = "Configuración guardada correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al guardar: " + ex.Message });
            }
        }
    }
}
