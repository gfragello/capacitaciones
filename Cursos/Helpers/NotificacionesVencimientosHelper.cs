using Cursos.Models;
using Cursos.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cursos.Helpers
{
    public sealed class NotificacionesVencimientosHelper
    {
        private readonly static NotificacionesVencimientosHelper _instance = new NotificacionesVencimientosHelper();

        private CursosDbContext db = new CursosDbContext();

        public static NotificacionesVencimientosHelper GetInstance()
        {
            return _instance;
        }

        public void ActualizarNotificacionesVencimientos()
        {
            var registrosPorVencer = db.NotificacionVencimientos.Where(r => r.Estado == EstadoNotificacionVencimiento.NotificacionPendiente);

            //registrosPorVencer = registrosPorVencer.Where(r => r.NotificarRegistroPorVencer);

            //var registrosPorVencer = db.RegistroCapacitacion.Where(r => r.FechaVencimiento >= fechaInicioControlVencimientos &&
            //                                                            r.FechaVencimiento >= r.FechaVencimiento.AddDays(diasAnticipacionAviso)).ToList();

            //List<RegistroCapacitacion> registrosPorVencer = new List<RegistroCapacitacion>();

            foreach (var registroPorVencer in registrosPorVencer.ToList())
            {
                NotificacionVencimiento notificacion = new NotificacionVencimiento
                {
                    RegistroCapacitacion = registroPorVencer.RegistroCapacitacion,
                    MailNotificacionVencimiento = string.Empty,
                    Estado = EstadoNotificacionVencimiento.NotificacionPendiente
                };

                db.NotificacionVencimientos.Add(notificacion);
            }

            //si se agregó alguna notificación de vencimiento
            if (registrosPorVencer.ToList().Count > 0)
                db.SaveChanges();

            /*
            var registrosPorVencer = db.RegistroCapacitacion.AsQueryable();

            registrosPorVencer = registrosPorVencer.Where(r => r.NotificarRegistroPorVencer);

            //var registrosPorVencer = db.RegistroCapacitacion.Where(r => r.FechaVencimiento >= fechaInicioControlVencimientos &&
            //                                                            r.FechaVencimiento >= r.FechaVencimiento.AddDays(diasAnticipacionAviso)).ToList();

            //List<RegistroCapacitacion> registrosPorVencer = new List<RegistroCapacitacion>();

            foreach (var registroPorVencer in registrosPorVencer.ToList())
            {
                NotificacionVencimiento notificacion = new NotificacionVencimiento
                                                       {
                                                            RegistroCapacitacion = registroPorVencer,
                                                            MailNotificacionVencimiento = string.Empty,
                                                            Estado = EstadoNotificacionVencimiento.NotificacionPendiente
                                                        };

                db.NotificacionVencimientos.Add(notificacion);
            }

            //si se agregó alguna notificación de vencimiento
            if (registrosPorVencer.ToList().Count > 0)
                db.SaveChanges();
            */
        }
    }
}