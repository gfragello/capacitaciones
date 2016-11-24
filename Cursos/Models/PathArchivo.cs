using Cursos.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
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
        public string SubDirectorio { get; set; }
        public TiposArchivo TipoArchivo { get; set; }

        [NotMapped]
        public string PathCompleto
        {
            get
            {
                return Path.Combine(this.SubDirectorio, this.NombreArchivo);
            }
        }
    }
}