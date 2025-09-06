using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    [Table("DocumentosInteres")]
    public class DocumentoInteres
    {
        public int DocumentoInteresID { get; set; }

        [Required(ErrorMessage = "Debe ingresar el {0}")]
        [Display(Name = "Nombre")]
        [MaxLength(200)]
        public string Nombre { get; set; }

        [Display(Name = "Descripción")]
        [MaxLength(1000)]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "Debe ingresar el {0}")]
        [Display(Name = "Nombre del Archivo")]
        [MaxLength(255)]
        public string NombreArchivo { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;
    }
}
