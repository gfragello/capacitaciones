using Cursos.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    [Table("PathsArchivos")]
    public class PathArchivo
    {
        public int PathArchivoId { get; set; }
        [StringLength(255)]
        public string NombreArchivo { get; set; }
        public TiposArchivo TipoArchivo { get; set; }
    }
}