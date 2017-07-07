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

namespace Cursos.Controllers
{
    public class NotificacionesVencimientosController : Controller
    {
        private CursosDbContext db = new CursosDbContext();

        // GET: NotificacionesVencimientos
        public ActionResult Index()
        {
            return View(ObtenerNotificacionesVencimiento(null));
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

                foreach (var emailCC in ConfiguracionHelper.GetInstance().GetValue("EmailNotificacionCC").Split(','))
                {
                    message.CC.Add(new MailAddress(emailCC));
                }
            
                message.From = new MailAddress("notificaciones@csl.uy");  // replace with valid value
                message.Subject = string.Format("{0} - {1} {2} {3}",
                                                ConfiguracionHelper.GetInstance().GetValue("EmailNotificacionAsunto"),
                                                empresa.NombreFantasia,
                                                DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString());
                message.Body = ConfiguracionHelper.GetInstance().GetValue("EmailNotificacionCuerpo");
                message.IsBodyHtml = true;

                message.Body += AgregarTableCapacitados(ObtenerNotificacionesVencimiento(empresaId));

                using (var smtp = new SmtpClient())
                {
                    var credential = new NetworkCredential
                    {
                        UserName = ConfiguracionHelper.GetInstance().GetValue("EmailUsuario"),
                        Password = ConfiguracionHelper.GetInstance().GetValue("PasswordUsuario")
                        //UserName = "notificaciones@csl.uy",
                        //Password = "n0tiFic@c1on3s"
                    };

                    smtp.Credentials = credential;
                    smtp.Host = ConfiguracionHelper.GetInstance().GetValue("SMPTHost");
                    smtp.Port = int.Parse(ConfiguracionHelper.GetInstance().GetValue("SMTPPort"));
                    smtp.EnableSsl = bool.Parse(ConfiguracionHelper.GetInstance().GetValue("SMTPSSL"));

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
            html += n.RegistroCapacitacion.FechaVencimiento.ToShortDateString();
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
            List<NotificacionVencimiento> notificacionVencimientosRet = new List<NotificacionVencimiento>();

            ActualizarNotificacionesVencimientos();

            int antelacionNotificacion = int.Parse(ConfiguracionHelper.GetInstance().GetValue("AntelacionNotificacion"));

            DateTime proximaFechaVencimientoNotificar = DateTime.Now.AddDays(antelacionNotificacion);

            MarcarRegistrosYaActualizados(proximaFechaVencimientoNotificar);

            var notificacionVencimientos = db.NotificacionVencimientos
                                             .Where(n => n.Estado == EstadoNotificacionVencimiento.NotificacionPendiente)
                                             .Where(n => n.RegistroCapacitacion.FechaVencimiento <= proximaFechaVencimientoNotificar)
                                             .OrderBy(n => n.RegistroCapacitacion.Jornada.Curso.CursoID)
                                             .OrderBy(n => n.RegistroCapacitacion.Capacitado.Empresa.NombreFantasia)
                                             .Include(n => n.RegistroCapacitacion);

            if (empresaId != null)
                notificacionVencimientos = notificacionVencimientos.Where(n => n.RegistroCapacitacion.Capacitado.EmpresaID == empresaId);

            return notificacionVencimientos.ToList();
        }

        //Las notificaciones asociadas a los registros no se crean al crear el registro
        //Antes de listar en pantalla los vencimientos, se le asocian la notificaciones a los registros que no la tienen
        private void ActualizarNotificacionesVencimientos()
        {
            using (CursosDbContext db = new CursosDbContext())
            {

                var rcSinNotificacioneAsociadas =
                db.RegistroCapacitacion.Where(r => !db.NotificacionVencimientos.Any(n => n.RegistroCapacitacionID == r.RegistroCapacitacionID)).ToList();

                if (rcSinNotificacioneAsociadas.Count > 0)
                {
                    foreach (var r in rcSinNotificacioneAsociadas)
                    {
                        var n = new NotificacionVencimiento
                        {
                            Estado = EstadoNotificacionVencimiento.NotificacionPendiente,
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

        //se marcan para no enviar aquellos registros de cursos que los capacitados ya actualizaron
        private void MarcarRegistrosYaActualizados(DateTime proximaFechaVencimientoNotificar)
        {
            List<int> notificacionVencimientosIds = null;
            List<int> capacitadosIds = null;
            List<int> jornadasVencidasId = null;

            //se obtienen los Ids de las notificaciones que están por vencer
            using (CursosDbContext db = new CursosDbContext())
            {
                var notificacionVencimientos = db.NotificacionVencimientos
                                                 .Where(n => n.Estado == EstadoNotificacionVencimiento.NotificacionPendiente)
                                                 .Where(n => n.RegistroCapacitacion.FechaVencimiento <= proximaFechaVencimientoNotificar);

                notificacionVencimientosIds = notificacionVencimientos.Select(n => n.NotificacionVencimientoID).ToList();
                capacitadosIds = notificacionVencimientos.Select(n => n.RegistroCapacitacion.Capacitado.CapacitadoID).ToList();
                jornadasVencidasId = notificacionVencimientos.Select(n => n.RegistroCapacitacion.JornadaID).ToList();
            }

            int i = 0;

            foreach (var jornadaVencidaId in jornadasVencidasId)
            {
                using (CursosDbContext db = new CursosDbContext())
                {
                    var jornadaVencida = db.Jornada.Find(jornadaVencidaId);
                    //var capacitado = db.Capacitados.Find(capacitadosIds[i]);

                    int currentCapacitadoId = capacitadosIds[i];
                    var capacitado = db.Capacitados.Where(c => c.CapacitadoID == currentCapacitadoId)
                                                   .Include(c => c.RegistrosCapacitacion).FirstOrDefault();

                    //se obtiene la última jornada asociada al curso de la jornada que se debería notificar como vencida
                    var ultimaJornadaCursoRegistrada =
                    capacitado.RegistrosCapacitacion.Where(r => r.Jornada.CursoId == jornadaVencida.CursoId).OrderByDescending(r => r.Jornada.Fecha).First();

                    //si la última jornada que tomó el capacitado no coincide con la que se va a notificar
                    //es porque el capacitado ya tomó una jornada de actualización por lo que no es necesario notificar ese vencimiento
                    if (ultimaJornadaCursoRegistrada.JornadaID != jornadaVencida.JornadaID)
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
