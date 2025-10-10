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

                n.Estado = EstadoNotificacionVencimiento.Notificado;
                n.Fecha = DateTime.Now;
                db.Entry(n).State = EntityState.Modified;
            }

            html += CerrarTableCapacitados();

            return html;
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
            //List<NotificacionVencimiento> notificacionVencimientosRet = new List<NotificacionVencimiento>();

            //TODO: NotificacionesVencimiento-Update
            //las actualizaciones de las notificaciones se realizan dentro de esta función
            //se debería en este punto crear las notificaciones en estado EstadoNotificacionVencimiento.NoNotificarYaActualizado
            //en los casos en los que corresponda
            ActualizarNotificacionesVencimientos();

            int antelacionNotificacion = int.Parse(ConfiguracionHelper.GetInstance().GetValue("AntelacionNotificacion", "Notificaciones"));

            DateTime proximaFechaVencimientoNotificar = DateTime.Now.AddDays(antelacionNotificacion);

            //MarcarRegistrosYaActualizados(proximaFechaVencimientoNotificar);

            //TODO 20181214 - Agregar un atributo en los cursos indicando si se notifican los vencimientos
            //No se notifican los vencimientos de las certificaciones correpondientes a cursos de Refresh (CursoId != 2), Inducción (CursoId != 4) y Viveros (CursoId != 5)
            var notificacionVencimientos = db.NotificacionVencimientos
                                             .Where(n => n.RegistroCapacitacion.Jornada.CursoId != 2 && n.RegistroCapacitacion.Jornada.CursoId != 4 && n.RegistroCapacitacion.Jornada.CursoId != 5)
                                             .Where(n => n.Estado == EstadoNotificacionVencimiento.NotificacionPendiente)
                                             .Where(n => n.RegistroCapacitacion.FechaVencimiento.HasValue && n.RegistroCapacitacion.FechaVencimiento.Value <= proximaFechaVencimientoNotificar)
                                             .OrderBy(n => n.RegistroCapacitacion.Jornada.Curso.CursoID)
                                             .OrderBy(n => n.RegistroCapacitacion.Capacitado.Empresa.NombreFantasia)
                                             .Include(n => n.RegistroCapacitacion);

            if (empresaId != null)
                notificacionVencimientos = notificacionVencimientos.Where(n => n.RegistroCapacitacion.Capacitado.EmpresaID == empresaId);

            //int totalRegistros = notificacionVencimientos.Count();
            bool generarArchivoIds = bool.Parse(ConfiguracionHelper.GetInstance().GetValue("GenerarArchivoIds", "Notificaciones"));

            if (generarArchivoIds)
            {
                LogHelper.GetInstance().WriteMessage("notificaciones", "------------------------------------");

                //se escriben los ids de las notificaciones
                LogHelper.GetInstance().WriteMessage("notificaciones", "NotificacionVencimientoID:");
                foreach (NotificacionVencimiento n in notificacionVencimientos)
                {
                    LogHelper.GetInstance().WriteMessage("notificaciones", n.NotificacionVencimientoID.ToString());
                }

                //se escriben los ids de los registros de capacitación
                LogHelper.GetInstance().WriteMessage("notificaciones", "RegistroCapacitacionID:");
                foreach (NotificacionVencimiento n in notificacionVencimientos)
                {
                    LogHelper.GetInstance().WriteMessage("notificaciones", n.RegistroCapacitacionID.ToString());
                }

                LogHelper.GetInstance().WriteMessage("notificaciones", "------------------------------------");
            }

            var notificacionVencimientosRet = notificacionVencimientos.ToList();
            notificacionVencimientosRet.RemoveAll(n => n.Estado == EstadoNotificacionVencimiento.NoNotificarYaActualizado);

            return notificacionVencimientosRet;
        }

        //Las notificaciones asociadas a los registros no se crean al crear el registro
        //Antes de listar en pantalla los vencimientos, se le asocian la notificaciones a los registros que no la tienen
        private void ActualizarNotificacionesVencimientos()
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {

                var rcSinNotificacioneAsociadas =
                db.RegistroCapacitacion.Where(r => r.FechaVencimiento.HasValue && !db.NotificacionVencimientos.Any(n => n.RegistroCapacitacionID == r.RegistroCapacitacionID)).ToList();

                if (rcSinNotificacioneAsociadas.Count > 0)
                {
                    foreach (var r in rcSinNotificacioneAsociadas)
                    {
                        EstadoNotificacionVencimiento estadoNotificacion =
                        VerificarCursoYaActualizado(r) ? EstadoNotificacionVencimiento.NoNotificarYaActualizado : EstadoNotificacionVencimiento.NotificacionPendiente;

                        if (estadoNotificacion == EstadoNotificacionVencimiento.NoNotificarYaActualizado)
                        {
                            Capacitado c = r.Capacitado;
                            Jornada jv = r.Jornada; //jornada vencida

                            string mensajelog =
                            string.Format("{0} {1} - {2}. No se notificará el vecimiento de la jornada {3} porque el Capacitado ya tiene una jornada posterior correspondiente a ese curso.",
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
                            Fecha = DateTime.Now
                        };

                        db.NotificacionVencimientos.Add(n);
                    }

                    db.SaveChanges();
                }
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

        //se marcan para no enviar aquellos registros de cursos que los capacitados ya actualizaron
        //TODO - 20190315 - Revisar esta función. Se comenta su única invocación desde la función ObtenerNotificacionesVencimiento 
        private void MarcarRegistrosYaActualizados(DateTime proximaFechaVencimientoNotificar)
        {
            List<int> notificacionVencimientosIds = null;
            List<int> capacitadosIds = null;
            List<int> jornadasVencidasId = null;

            //se obtienen los Ids de las notificaciones que están por vencer
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                var notificacionVencimientos = db.NotificacionVencimientos
                                                 .Where(n => n.Estado == EstadoNotificacionVencimiento.NotificacionPendiente)
                                                 .Where(n => n.RegistroCapacitacion.FechaVencimiento.HasValue && n.RegistroCapacitacion.FechaVencimiento.Value <= proximaFechaVencimientoNotificar);

                notificacionVencimientosIds = notificacionVencimientos.Select(n => n.NotificacionVencimientoID).ToList();
                capacitadosIds = notificacionVencimientos.Select(n => n.RegistroCapacitacion.Capacitado.CapacitadoID).ToList();
                jornadasVencidasId = notificacionVencimientos.Select(n => n.RegistroCapacitacion.JornadaID).Distinct().ToList();
            }

            int i = 0;

            foreach (var jornadaVencidaId in jornadasVencidasId)
            {
                using (ApplicationDbContext db = new ApplicationDbContext())
                {
                    var jornadaVencida = db.Jornada.Find(jornadaVencidaId);
                    //var capacitado = db.Capacitados.Find(capacitadosIds[i]);

                    int currentCapacitadoId = capacitadosIds[i];
                    var capacitado = db.Capacitados.Where(c => c.CapacitadoID == currentCapacitadoId)
                                                   .Include(c => c.RegistrosCapacitacion).FirstOrDefault();

                    //se obtiene la última jornada asociada al curso de la jornada que se debería notificar como vencida
                    var ultimaJornadaCursoRegistrada =
                    capacitado.RegistrosCapacitacion.Where(r => r.Jornada.CursoId == jornadaVencida.CursoId).OrderByDescending(r => r.Jornada.Fecha).FirstOrDefault();

                    //si la última jornada que tomó el capacitado no coincide con la que se va a notificar
                    //es porque el capacitado ya tomó una jornada de actualización por lo que no es necesario notificar ese vencimiento
                    if (ultimaJornadaCursoRegistrada != null && ultimaJornadaCursoRegistrada.JornadaID != jornadaVencida.JornadaID)
                    {
                        var notificacionVencimiento = db.NotificacionVencimientos.Find(notificacionVencimientosIds[i]);
                        notificacionVencimiento.Estado = EstadoNotificacionVencimiento.NoNotificar;

                        string mensajelog =
                        string.Format("{0} - {1}. No se notificará el vecimiento de la jornada {2} porque el Capacitado ya tiene una jornada posterior correspondiente a ese curso.",
                                      capacitado.DocumentoCompleto,
                                      capacitado.NombreCompleto,
                                      jornadaVencida.JornadaIdentificacionCompleta);

                        LogHelper.GetInstance().WriteMessage("notificaciones", mensajelog);
                        db.SaveChanges();
                    }
                }

                i++;
            }
            /*
            using (CursosDbContext db = new CursosDbContext())
            {
                var notificacionVencimientos = db.NotificacionVencimientos
                                                 .Where(n => n.Estado == EstadoNotificacionVencimiento.NotificacionPendiente)
                                                 .Where(n => n.RegistroCapacitacion.FechaVencimiento <= proximaFechaVencimientoNotificar)
                                                 .Include(n => n.RegistroCapacitacion)
                                                 .Include(n => n.RegistroCapacitacion.Capacitado.RegistrosCapacitacion);

                if (notificacionVencimientos.Count() > 0)
                {
                    //se verifica que el capacitado no tenga jornadas posteriores a la 
                    foreach (var n in notificacionVencimientos)
                    {
                        var capacitado = n.RegistroCapacitacion.Capacitado;
                        var jornadaVencida = n.RegistroCapacitacion.Jornada;

                        //se obtiene la última jornada asociada al curso de la jornada que se debería notificar como vencida
                        var ultimaJornadaCursoRegistrada =
                        capacitado.RegistrosCapacitacion.Where(r => r.Jornada.CursoId == jornadaVencida.CursoId).OrderByDescending(r => r.Jornada.Fecha).First();

                        //si la última jornada que tomó el capacitado no coincide con la que se va a notificar
                        //es porque el capacitado ya tomó una jornada de actualización por lo que no es necesario notificar ese vencimiento
                        if (ultimaJornadaCursoRegistrada.JornadaID != jornadaVencida.JornadaID)
                            n.Estado = EstadoNotificacionVencimiento.NoNotificar;
                    }

                    //db.SaveChanges();
                }
            }
            */
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
                                     where rc.FechaVencimiento.HasValue && rc.FechaVencimiento.Value > fechaDesde && !new int[] { 2, 4, 5 }.Contains(cu.CursoID)
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
    }
}
