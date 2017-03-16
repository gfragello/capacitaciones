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

         //el parámetro jornadaIdExcluir indica que los capacitados que participaron de esas jornadas no pueden ser seleccionados
        [HttpGet]
        public ActionResult NotificacionesVencimientosAdvertencia()
        {
            /*
            var capacitados = db.Capacitados.Include(c => c.Empresa).Include(c => c.RegistrosCapacitacion);

            if (!String.IsNullOrEmpty(documento))
                capacitados = capacitados.Where(c => c.Documento.Contains(documento));

            ViewBag.JornadaIdExcluir = jornadaIdExcluir;
            */
            NotificacionesVencimientosHelper.GetInstance().ActualizarNotificacionesVencimientos();

            int totalNotificacionesPendientes = db.NotificacionVencimientos.Where(n => n.Estado == EstadoNotificacionVencimiento.NotificacionPendiente).Count();

            if (totalNotificacionesPendientes > 0)
            {
                ViewBag.TotalNotificacionesPendientes = totalNotificacionesPendientes;
                return PartialView("_NotificacionesVencimientosAdvertenciaPartial");
            }

            return Content("");
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

        private bool EnviarEmailsEmpresa(int empresaId)
        {
            var empresa = db.Empresas.Where(e => e.EmpresaID == empresaId).FirstOrDefault();

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
                    UserName = "notificaciones@csl.uy",
                    Password = "n0tiFic@c1on3s"
                    //UserName = "guillefra@gmail.com",  // replace with valid value
                    //Password = "puppet250139"  // replace with valid value
                };

                /*
                smtp.Credentials = credential;
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.EnableSsl = true;
                */

                smtp.Credentials = credential;
                smtp.Host = "smtp.zoho.com";
                smtp.Port = 587;
                smtp.EnableSsl = true;

                try
                {
                    smtp.Send(message);
                    db.SaveChanges(); //se modificó el estado de las notificaciones a "Ënviada"

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
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
            var notificacionVencimientos = db.NotificacionVencimientos
                                 .Where(n => n.Estado == EstadoNotificacionVencimiento.NotificacionPendiente)
                                 .OrderBy(n => n.RegistroCapacitacion.Jornada.Curso.CursoID)
                                 .OrderBy(n => n.RegistroCapacitacion.Capacitado.Empresa.NombreFantasia)
                                 .Include(n => n.RegistroCapacitacion);

            if (empresaId != null)
                notificacionVencimientos = notificacionVencimientos.Where(n => n.RegistroCapacitacion.Capacitado.EmpresaID == empresaId);

            return notificacionVencimientos.ToList();
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
