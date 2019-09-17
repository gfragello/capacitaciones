using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    [Table("InstructoresUsuarios")]
    public class InstructorUsuario
    {
        public int InstructorUsuarioId { get; set; }
        public string Usuario { get; set; }

        [Display(Name = "Capacitado")]
        public int InstructorID { get; set; }
        public virtual Instructor Instructor { get; set; }
    }
}