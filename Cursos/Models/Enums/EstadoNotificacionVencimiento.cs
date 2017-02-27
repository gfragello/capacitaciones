using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Cursos.Models.Enums
{
    public enum EstadoNotificacionVencimiento
    {
        [Display(Name = "Notificacion Pendiente")]
        NotificacionPendiente = 0, //valor por defecto
        Notificado,
        [Display(Name = "No Notificar")]
        NoNotificar
    }
}