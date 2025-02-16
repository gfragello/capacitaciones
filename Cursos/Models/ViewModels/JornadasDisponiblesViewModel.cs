using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cursos.Models;

namespace Cursos.Models.ViewModels
{
    public class JornadasDisponiblesViewModel
    {
        public IEnumerable<Jornada> Jornadas { get; set; }

        // Mensajes asociados a la vista
        public MensajeUsuario MensajeDisponibles1 { get; set; }
        public MensajeUsuario MensajeDisponibles2 { get; set; }
    }
}