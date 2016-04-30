using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    [Table("Departamentos")]
    public class Departamento
    {
        public int DepartamentoID { get; set; }

        [MaxLength(25)]
        public string Nombre { get; set; }
    }
}