using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Mvc;

namespace Cursos.Models
{
    [Table("MensajesUsuarios")]
    public class MensajeUsuario
    {
        [Key]
        public int MensajesUsuariosID { get; set; }

        public string IdentificadorInterno { get; set; }

        [Required]
        [AllowHtml]
        public string Cuerpo { get; set; }
    }
}