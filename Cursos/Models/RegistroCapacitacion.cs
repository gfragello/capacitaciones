using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    [Table("RegistrosCapacitaciones")]
    public class RegistroCapacitacion : ElementoAccesoControlado
    {
        //CONS: Acalarale a Alejandro que fue necesario cambiar el nombre de esta tabla
        //CONS: Consultar si durante la prueba de la etapa 1Alejandro quiere tener acceso a la base de datos a través de un ciente
        //      Enterprise Studio Express
        public int RegistroCapacitacionID { get; set; }

        //CONS: Los atributos Fecha, LugarID, CursoID, InstitucionID no son necesario porque se acceden directamente a través de la 
        //      referencia que existe a JornadaID. Verificar que la columna RegistroCapacitacion.Fecha se coresponda con la columna
        //      Jornada.Fecha

        [Required(AllowEmptyStrings = false)]
        public bool Aprobado { get; set; }

        [NotMapped]
        public string AprobadoTexto
        {
            get
            {
                if (this.Aprobado)
                    return "Aprobado";
                else
                    return "No Aprobado";
            }    
        }

        [Required(ErrorMessage = "Debe ingresar la nota obtenida")]
        public int Nota { get; set; }

        [Display(Name = "Nota previa")]
        public int? NotaPrevia { get; set; }

        [Required(ErrorMessage = "Debe seleccionar la Jornada")]
        [Display(Name = "Jornada")]
        public int JornadaID { get; set; }
        public virtual Jornada Jornada { get; set; }

        public int CapacitadoID { get; set; }
        public virtual Capacitado Capacitado { get; set; }

        [Display(Name = "Fecha Vencimiento")]
        [Required(ErrorMessage = "Debe ingresar la {0}")]
        public DateTime FechaVencimiento { get; set; }

        /*
        public int EmpresaID { get; set; }
        public virtual Empresa Empresa { get; set; }        
        */
    }
}