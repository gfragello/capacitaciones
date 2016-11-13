using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Cursos.Models.Enums
{
    public enum TiposArchivo
    {
        [Display(Name = "Foto del Capacitado")]
        FotoCapacitado = 0, //valor por defecto
        ActaEscaneada
    }
}