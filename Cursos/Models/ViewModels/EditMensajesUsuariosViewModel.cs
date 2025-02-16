using System.ComponentModel.DataAnnotations;
using Cursos.Models;  // Para acceder a la clase MensajeUsuario

namespace Cursos.Models.ViewModels
{
    public class EditMensajesUsuariosViewModel
    {
        [Required]
        public MensajeUsuario Mensaje1 { get; set; }

        [Required]
        public MensajeUsuario Mensaje2 { get; set; }
    }
}
