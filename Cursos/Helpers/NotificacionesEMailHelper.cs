using Cursos.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace Cursos.Helpers
{
    public class NotificacionesEMailHelper
    {
        private readonly static NotificacionesEMailHelper _instance = new NotificacionesEMailHelper();

        public static NotificacionesEMailHelper GetInstance()
        {
            return _instance;
        }

        public bool EnviarEmailsNotificacionInscripcionExterna(RegistroCapacitacion registroCapacitacion, bool capacitadoCreado)
        {
            //var registroCapacitacion = db.RegistroCapacitacion.Where(r => r.RegistroCapacitacionID == registroCapacitacionId).FirstOrDefault();

            if (registroCapacitacion != null)
            {
                var message = new MailMessage();

                foreach (var emailDestinatario in ConfiguracionHelper.GetInstance().GetValue("EmailInscripcionEDestinatario", "Inscripciones_Externas").Split(','))
                {
                    message.To.Add(new MailAddress(emailDestinatario));
                }

                message.From = new MailAddress(ConfiguracionHelper.GetInstance().GetValue("EmailUsuario", "Inscripciones_Externas"));

                //en el subject del mail se agrega un valor randómico para evitar que los mensajes se muestren anidados en los clientes web mail
                message.Subject = string.Format(ConfiguracionHelper.GetInstance().GetValue("EmailInscripcionEAsunto", "Inscripciones_Externas"), registroCapacitacion.Jornada.JornadaIdentificacionCompleta, this.GenerateRandomicoSubjectMail());

                UrlHelper url = new UrlHelper(HttpContext.Current.Request.RequestContext);

                message.Body = string.Format(ConfiguracionHelper.GetInstance().GetValue("EmailInscripcionECuerpo", "Inscripciones_Externas"),
                                             System.Web.HttpContext.Current.User.Identity.Name,
                                             registroCapacitacion.Capacitado.NombreCompleto,
                                             registroCapacitacion.Capacitado.TipoDocumento.Abreviacion,
                                             registroCapacitacion.Capacitado.Documento,
                                             registroCapacitacion.Capacitado.Empresa.NombreFantasia,
                                             registroCapacitacion.Jornada.JornadaIdentificacionCompleta,
                                             url.Action("Details", "Jornadas", new { id = registroCapacitacion.JornadaID }, "http"));

                string datosCapacitado = string.Empty;

                if (capacitadoCreado)
                    datosCapacitado = "<p style=\"color: red; font-weight: bold;\">ATENCIÓN - El capacitado fue agregado por el usuario. <a href=\"{0}\">Ver capacitado</a></p>";
                else
                    datosCapacitado = "<p>El capacitado ya existía en la base de datos. <a href=\"{0}\">Ver capacitado</a></p>";

                message.Body += string.Format(datosCapacitado, url.Action("Details", "Capacitados", new { id = registroCapacitacion.CapacitadoID }, "http"));

                message.IsBodyHtml = true;

                using (var smtp = new SmtpClient())
                {
                    var credential = new NetworkCredential
                    {
                        UserName = ConfiguracionHelper.GetInstance().GetValue("EmailUsuario", "Inscripciones_Externas"),
                        Password = ConfiguracionHelper.GetInstance().GetValue("PasswordUsuario", "Inscripciones_Externas")
                    };

                    smtp.Credentials = credential;
                    smtp.Host = ConfiguracionHelper.GetInstance().GetValue("SMPTHost", "Inscripciones_Externas");
                    smtp.Port = int.Parse(ConfiguracionHelper.GetInstance().GetValue("SMTPPort", "Inscripciones_Externas"));
                    smtp.EnableSsl = bool.Parse(ConfiguracionHelper.GetInstance().GetValue("SMTPSSL", "Inscripciones_Externas"));

                    try
                    {
                        smtp.Send(message);
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

        public bool EnviarEmailsNotificacionEliminacionInscripcion(RegistroCapacitacion registroCapacitacion)
        {
            if (registroCapacitacion != null)
            {
                var message = new MailMessage();

                foreach (var emailDestinatario in ConfiguracionHelper.GetInstance().GetValue("EmailEliminacionInscripcionEDestinatario", "Eliminacion_Inscripcion").Split(','))
                {
                    message.To.Add(new MailAddress(emailDestinatario));
                }

                message.From = new MailAddress(ConfiguracionHelper.GetInstance().GetValue("EmailUsuario", "Eliminacion_Inscripcion"));

                //en el subject del mail se agrega un valor randómico para evitar que los mensajes se muestren anidados en los clientes web mail
                message.Subject = string.Format(ConfiguracionHelper.GetInstance().GetValue("EmailEliminacionInscripcionEAsunto", "Eliminacion_Inscripcion"), registroCapacitacion.Jornada.JornadaIdentificacionCompleta, this.GenerateRandomicoSubjectMail());

                UrlHelper url = new UrlHelper(HttpContext.Current.Request.RequestContext);

                message.Body = string.Format(ConfiguracionHelper.GetInstance().GetValue("EmailEliminacionInscripcionECuerpo", "Eliminacion_Inscripcion"),
                                             System.Web.HttpContext.Current.User.Identity.Name,
                                             registroCapacitacion.Capacitado.NombreCompleto,
                                             registroCapacitacion.Capacitado.TipoDocumento.Abreviacion,
                                             registroCapacitacion.Capacitado.Documento,
                                             registroCapacitacion.Jornada.JornadaIdentificacionCompleta,
                                             url.Action("Details", "Jornadas", new { id = registroCapacitacion.JornadaID }, "http"));

                message.IsBodyHtml = true;

                using (var smtp = new SmtpClient())
                {
                    var credential = new NetworkCredential
                    {
                        UserName = ConfiguracionHelper.GetInstance().GetValue("EmailUsuario", "Eliminacion_Inscripcion"),
                        Password = ConfiguracionHelper.GetInstance().GetValue("PasswordUsuario", "Eliminacion_Inscripcion")
                    };

                    smtp.Credentials = credential;
                    smtp.Host = ConfiguracionHelper.GetInstance().GetValue("SMPTHost", "Eliminacion_Inscripcion");
                    smtp.Port = int.Parse(ConfiguracionHelper.GetInstance().GetValue("SMTPPort", "Eliminacion_Inscripcion"));
                    smtp.EnableSsl = bool.Parse(ConfiguracionHelper.GetInstance().GetValue("SMTPSSL", "Eliminacion_Inscripcion"));

                    try
                    {
                        smtp.Send(message);
                        return true;
                    }
                    catch (Exception e)
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

        public bool EnviarEmailJornadaCreacion(Jornada jornada)
        {
            if (jornada != null)
            {
                var message = new MailMessage();

                foreach (var emailDestinatario in ConfiguracionHelper.GetInstance().GetValue("EmailCreacionJornadaEDestinatario", "Notificaciones").Split(','))
                {
                    message.To.Add(new MailAddress(emailDestinatario));
                }

                message.From = new MailAddress(ConfiguracionHelper.GetInstance().GetValue("EmailUsuario", "Notificaciones"));

                //en el subject del mail se agrega un valor randómico para evitar que los mensajes se muestren anidados en los clientes web mail
                message.Subject = string.Format(ConfiguracionHelper.GetInstance().GetValue("EmailCreacionJornadaEAsunto", "Notificaciones"), this.GenerateRandomicoSubjectMail());

                UrlHelper url = new UrlHelper(HttpContext.Current.Request.RequestContext);

                message.Body = string.Format(ConfiguracionHelper.GetInstance().GetValue("EmailCreacionJornadaECuerpo", "Notificaciones"),
                                                System.Web.HttpContext.Current.User.Identity.Name,
                                                jornada.JornadaIdentificacionCompleta,
                                                url.Action("Details", "Jornadas", new { id = jornada.JornadaID }, "http"));

                message.IsBodyHtml = true;

                using (var smtp = new SmtpClient())
                {
                    var credential = new NetworkCredential
                    {
                        UserName = ConfiguracionHelper.GetInstance().GetValue("EmailUsuario", "Notificaciones"),
                        Password = ConfiguracionHelper.GetInstance().GetValue("PasswordUsuario", "Notificaciones")
                    };

                    smtp.Credentials = credential;
                    smtp.Host = ConfiguracionHelper.GetInstance().GetValue("SMPTHost", "Notificaciones");
                    smtp.Port = int.Parse(ConfiguracionHelper.GetInstance().GetValue("SMTPPort", "Notificaciones"));
                    smtp.EnableSsl = bool.Parse(ConfiguracionHelper.GetInstance().GetValue("SMTPSSL", "Notificaciones"));

                    try
                    {
                        smtp.Send(message);
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

        public bool EnviarEmailJornadaActa(Jornada jornada)
        {
            ApplicationDbContext db = new ApplicationDbContext();

            if (jornada != null && jornada.Curso != null)
            {
                var message = new MailMessage();

                // Obtener destinatarios desde la propiedad ActaEmail del curso
                if (!string.IsNullOrEmpty(jornada.Curso.ActaEmail))
                {
                    foreach (var emailDestinatario in jornada.Curso.ActaEmail.Split(','))
                    {
                        message.To.Add(new MailAddress(emailDestinatario.Trim()));
                    }
                }

                // Configurar remitente del correo
                message.From = new MailAddress(ConfiguracionHelper.GetInstance().GetValue("EmailUsuario", "Envio_Actas"));

                // Asunto del correo
                message.Subject = string.Format("Acta de la Jornada {0}", jornada.JornadaIdentificacionCompleta);

                // Cuerpo del correo: se parte del cuerpo base del curso
                string body = jornada.Curso.ActaEmailCuerpo;

                // Recuperar el usuario logueado y, si tiene firma, concatenarla
                var usuarioLogueado = db.Users.FirstOrDefault(u => u.UserName == HttpContext.Current.User.Identity.Name);
                if (usuarioLogueado != null && usuarioLogueado.HasSignatureFooter &&
                    !string.IsNullOrEmpty(usuarioLogueado.SignatureFooter))
                {
                    body += "<br/>" + usuarioLogueado.SignatureFooter;
                }
                message.Body = body;
                message.IsBodyHtml = true;

                // Adjuntar el archivo del acta generado por GenerarJornadaExcelStream
                var stream = ExportarExcelHelper.GetInstance().GenerarJornadaExcelStream(jornada);
                if (stream != null)
                {
                    stream.Position = 0; // Asegurarse de que el stream esté al inicio
                    var attachment = new Attachment(stream, $"{jornada.JornadaIdentificacionCompleta}.xlsx",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                    message.Attachments.Add(attachment);
                }
                else
                {
                    return false; // Si el stream es nulo, no se puede generar el archivo adjunto
                }

                // Configurar el cliente SMTP
                using (var smtp = new SmtpClient())
                {
                    var credential = new NetworkCredential
                    {
                        UserName = ConfiguracionHelper.GetInstance().GetValue("EmailUsuario", "Envio_Actas"),
                        Password = ConfiguracionHelper.GetInstance().GetValue("PasswordUsuario", "Envio_Actas")
                    };

                    smtp.Credentials = credential;
                    smtp.Host = ConfiguracionHelper.GetInstance().GetValue("SMPTHost", "Envio_Actas");
                    smtp.Port = int.Parse(ConfiguracionHelper.GetInstance().GetValue("SMTPPort", "Envio_Actas"));
                    smtp.EnableSsl = bool.Parse(ConfiguracionHelper.GetInstance().GetValue("SMTPSSL", "Envio_Actas"));

                    try
                    {
                        smtp.Send(message);

                        // Registrar el envío del acta
                        var registroEnvio = new JornadaActaEnviada
                        {
                            JornadaID = jornada.JornadaID,
                            UsuarioEnvio = HttpContext.Current.User.Identity.Name,
                            FechaHoraEnvio = DateTime.Now,
                            MailDestinoEnvio = jornada.Curso.ActaEmail // Si hay múltiples, podrías concatenarlos
                        };
                        db.JornadaActasEnviadas.Add(registroEnvio);
                        db.SaveChanges();

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

        private string GenerateRandomicoSubjectMail()
        {
            int _min = 0;
            int _max = 99999;

            Random _rdm = new Random();

            return _rdm.Next(_min, _max).ToString().Trim().PadLeft(5, Char.Parse("0"));
        }
    }
}