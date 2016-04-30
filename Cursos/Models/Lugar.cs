using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    [Table("Lugares")]
    public class Lugar
    {
        public int LugarID { get; set; }

        [Required(ErrorMessage = "Debe ingresar el nombre del lugar")]
        [Display(Name="Nombre")]
        public string NombreLugar { get; set; }

        //CONS: Establecer un mínimo y un máximo para la abreviación del lugar?
        [MaxLength(3)]
        [Display(Name = "Abreviación")]
        public string AbrevLugar { get; set; }

        public virtual List<Jornada> Jornadas { get; set; }
    }
}