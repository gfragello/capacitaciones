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

        [Display(Name = "Descripción en Inglés")]
        public string DescripcionEnIngles { get; set; }

        [NotMapped]
        public string DescripcionMultiLanguage
        {
            get
            {
                if (System.Threading.Thread.CurrentThread.CurrentUICulture.Name.ToString() == "en-US" && !string.IsNullOrEmpty(this.DescripcionEnIngles))
                    return this.DescripcionEnIngles;

                return this.Descripcion;
            }
        }

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

        [Required(ErrorMessage = "Debe ingresar el color asociado al curso")]
        [Display(Name = "Color")]
        public string ColorDeFondo { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Evaluación con nota")]
        public bool EvaluacionConNota { get; set; }

        [Display(Name = "Puntaje Máximo")]
        public int PuntajeMaximo { get; set; }

        [Display(Name = "Puntaje Mínimo")]
        public int PuntajeMinimo { get; set; }

        [Display(Name = "Requiere Autorización")]
        public bool RequiereAutorizacion { get; set; }

        [Display(Name = "Tiene mínimo de asistentes")]
        public bool TieneMinimoAsistentes { get; set; }

        [Required(ErrorMessage = "Debe ingresar la {0}")]
        [Display(Name = "Cantidad mínima de asistentes")]
        public int MinimoAsistentes { get; set; }

        [Display(Name = "Tiene máximo de asistentes")]
        public bool TieneMaximoAsistentes { get; set; }

        [Required(ErrorMessage = "Debe ingresar la {0}")]
        [Display(Name = "Cantidad máxima de asistentes")]
        public int MaximoAsistentes { get; set; }

        [Display(Name = "Tiene Cierre Incripción")]
        public bool TieneCierreIncripcion { get; set; }

        [Required(ErrorMessage = "Debe ingresar las {0}")]
        [Display(Name = "Horas previas para el cierre de incripción")]
        public int HorasCierreInscripcion { get; set; }

        [Display(Name = "Permite inscripciones externas")]
        public bool PermiteInscripcionesExternas { get; set; }

        [Display(Name = "Permite envíos OVAL")]
        public bool PermiteEnviosOVAL { get; set; }

        [Display(Name = "Punto de servicio")]
        public int? PuntoServicioId { get; set; }
        public virtual PuntoServicio PuntoServicio { get; set; }

        public virtual List<Jornada> Jornadas { get; set; }
    }
}