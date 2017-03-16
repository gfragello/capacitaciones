using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    [Table("Configuracion")]
    public class Configuracion
    {
        public int ConfiguracionID { get; set; }

        public string Index { get; set; }

        public string Value { get; set; }
    }
}