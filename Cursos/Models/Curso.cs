using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    [Table("Cursos")]
    public class Curso
    {
        public int CursoID { get; set; }

        [Required(ErrorMessage = "Debe ingresar la descripción del curso")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "Debe ingresar el costo del curso")]
        public int Costo { get; set; }

        //TODO: Topear el máximo de horas en 999
        //CONS: Ver si es correcto que el número máximo de horas sea 999
        [Required(ErrorMessage = "Debe ingresar la horas del curso")]
        public int Horas { get; set; }

        [Display(Name = "Años de vigencia")]
        [Required(ErrorMessage = "Debe ingresar los {0} del curso")]
        public int Vigencia { get; set; }

        [Required(ErrorMessage = "Debe ingresar el módulo del curso")]
        public string Modulo { get; set; }

        [Display(Name = "Color de Fondo")]
        public string ColorDeFondo { get; set; }

        //TODO: agregar las siguientes propiedades en las pantallas de edición y creación de cursos

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Evaluación con nota")]
        public bool EvaluacionConNota { get; set; }

        [Display(Name = "Puntaje Máximo")]
        public int PuntajeMaximo { get; set; }

        [Display(Name = "Puntaje Mínimo")]
        public int PuntajeMinimo { get; set; }

        public virtual List<Jornada> Jornadas { get; set; }
    }
}