using System;
using System.ComponentModel.DataAnnotations;

namespace Cursos.Models
{
    public class JornadaActaEnviada
    {
        public int JornadaActaEnviadaID { get; set; }

        // Relación con Jornada (clave foránea)
        [Required]
        public int JornadaID { get; set; }

        public virtual Jornada Jornada { get; set; }

        // Usuario que envió el acta
        [Required]
        public string UsuarioEnvio { get; set; }

        // Fecha y hora en que se envió el acta
        [Required]
        public DateTime FechaHoraEnvio { get; set; }

        // Email (o emails) a los que se envió el acta
        [Required]
        public string MailDestinoEnvio { get; set; }
    }
}
