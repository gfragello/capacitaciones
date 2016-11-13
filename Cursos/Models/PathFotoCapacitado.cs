using Cursos.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    public class PathFotoCapacitado
    {
        public int PathFotoCapacitadoId { get; set; }
        [StringLength(255)]
        public string NombreArchivo { get; set; }
        public TiposArchivo TipoArchivo { get; set; }

        public int CapacitadoID { get; set; }
        public virtual Capacitado Capacitado { get; set; }
    }
}