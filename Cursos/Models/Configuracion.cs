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

        public string Label { get; set; }

        public int Order { get; set; }

        public string Seccion { get; set; }

        public string SubSeccion { get; set; }
    }
}