using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    [Table("EmpresaUsuarios")]
    public class EmpresaUsuario
    {
        public int EmpresaUsuarioId { get; set; }
        public string Usuario { get; set; }

        [Display(Name = "Empresa")]
        public int EmpresaID { get; set; }
        public virtual Empresa Empresa { get; set; }
    }
}