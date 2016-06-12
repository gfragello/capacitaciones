using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    [Table("Instructores")]
    public class Instructor : ElementoAccesoControlado
    {
        public int InstructorID { get; set; }

        [Required(ErrorMessage = "Debe ingresar el {0} del instructor")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Debe ingresar el {0} del instructor")]
        public string Apellido { get; set; }

        [NotMapped]
        public string NombreCompleto
        {
            get
            {
                return string.Format("{0} {1}", this.Nombre, this.Apellido);
            }
        }

        public string Documento { get; set; }

        [Display(Name = "Fecha de Nacimiento")]
        [DataType(DataType.Date)]
        public DateTime FechaNacimiento { get; set; }

        public string Domicilio { get; set; }

        [DataType(DataType.PhoneNumber, ErrorMessage = "Ël número de teléfono ingresado no es válido")]
        public string Telefono { get; set; }

        //TODO: ver como se va a resolver el almacenamiento en un blob
        //public string Foto { get; set; }

        //TODO: en el código generado por CFM ponerle a esta atributo el valor "true" por defecto
        //ejemplo: AddColumn("dbo.Events", "Active", c => c.Boolean(nullable: false, defaultValue: true));
        public bool Activo { get; set; }

        public virtual List<Jornada> Jornadas { get; set; }
    }
}