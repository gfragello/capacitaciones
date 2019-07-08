using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Cursos.Models.Enums
{
    public enum EstadosEnvioOVAL
    { 
        [Display(Name = "No enviar")]
        NoEnviar = 0,
        [Display(Name = "Pendiente de envío")]
        PendienteEnvio,
        Aceptado,
        Rechazado
    }
}