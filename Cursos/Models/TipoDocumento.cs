using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cursos.Models
{ 
    [Table("TiposDocumento")]
    public class TipoDocumento
    {
        public int TipoDocumentoID { get; set; }

        [Required(ErrorMessage = "Debe ingresar la {0}")]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        [MaxLength(4)]
        public string Abreviacion { get; set; }

        public virtual List<Capacitado> Capacitados { get; set; }
    }
}