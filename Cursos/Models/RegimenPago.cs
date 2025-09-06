using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    [Table("RegimenesPago")]
    public class RegimenPago
    {
        public int RegimenPagoID { get; set; }

        [Required(ErrorMessage = "Debe ingresar el {0}")]
        [Display(Name = "Nombre del régimen")]
        public string Nombre { get; set; }

        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        public virtual ICollection<Empresa> Empresas { get; set; }
    }
}
