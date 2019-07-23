using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    public class RegistroOVAL
    {
        public string TipoDocumento { get; set; }
        public string NumeroDocumento { get; set; }
        public string TipoInduccion { get; set; }
        public bool Aprobado { get; set; }
        public DateTime FechaInduccion { get; set; }
        public string Notas { get; set; }
    }
}