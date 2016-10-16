using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Cursos.Models.Enums
{
    public enum EstadosRegistroCapacitacion
    {
        Inscripto = 0, //valor por defecto
        Aprobado,
        [Display(Name = "No aprobado")]
        NoAprobado
    };
}