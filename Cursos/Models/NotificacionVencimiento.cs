using Cursos.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    [Table("NotificacionesVencimientos")]
    public class NotificacionVencimiento
    {
        public int NotificacionVencimientoID { get; set; }

        public DateTime Fecha { get; set; }

        public string MailNotificacionVencimiento { get; set; }

        public EstadoNotificacionVencimiento Estado { get; set; }

        public int RegistroCapacitacionID { get; set; }
        public virtual RegistroCapacitacion RegistroCapacitacion { get; set; }
    }
}