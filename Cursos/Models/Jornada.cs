using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    public class Jornada : ElementoAccesoControlado
    {
        public int JornadaID { get; set; }

        [Required(ErrorMessage = "Debe ingresar la fecha del curso")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime Fecha { get; set; }

        [Required(ErrorMessage = "Debe ingresar la Hora de la Jornada")]
        [RegularExpression(@"([01]?[0-9]|2[0-3]):[0-5][0-9]", ErrorMessage = "Hora no válida.")]
        public string Hora { get; set; }

        [Display(Name = "Lugar")]
        public int LugarID { get; set; }
        public virtual Lugar Lugar { get; set; }

        [Display(Name = "Curso")]
        public int CursoId { get; set; }
        public virtual Curso Curso { get; set; }

        [Display(Name = "Instructor")]
        public int InstructorId { get; set; }
        public virtual Instructor Instructor { get; set; }

        public virtual List<RegistroCapacitacion> RegistrosCapacitacion { get; set; }

        [NotMapped]
        public string JornadaIdentificacionCompleta
        {
            get
            {
                return String.Format("{0} - {1} {2} - {3}", Curso.Descripcion, this.Fecha.ToShortDateString(), this.Hora, this.Lugar.AbrevLugar);
            }
        }

        [NotMapped]
        public string FechaFormatoYYYYYMMDD
        {
            get
            {
                return String.Format("{0}{1}{2}", this.Fecha.Year.ToString(), this.Fecha.Month.ToString(), this.Fecha.Year.ToString());
            }
        }

        public DateTime ObtenerFechaVencimiento()
        {
            //TODO: hacer que esto sea configurable
            //que se pueda especificar en el curso que vence el último día del año
            //Tener en cuenta que al usar esta función desde la funcionalidad Copiar Jornada, this.Curso
            //no estará instanciado
            if (this.CursoId == 2)
                return new DateTime(this.Fecha.Year, 12, 31);
            else
                return new DateTime(this.Fecha.Year + this.Curso.Vigencia, this.Fecha.Month, this.Fecha.Day);
        }
    }
}