using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    [NotMapped]
    public class ArchivoLog
    {
        public string Nombre { get; set; }
        public string Path { get; set; }
    }
}