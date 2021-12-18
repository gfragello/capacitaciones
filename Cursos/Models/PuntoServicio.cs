using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using Cursos.Models.Enums;

namespace Cursos.Models
{
    [Table("PuntosServicio")]
    public class PuntoServicio
    {
        public int PuntoServicioId { get; set; }

        [Required(ErrorMessage = "Debe ingresar el nombre")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Debe ingresar la dirección")]
        public string Direccion { get; set; }

        public string Usuario { get; set; }

        public string Password { get; set; }

        public TipoPuntoServicio Tipo { get; set; }

        public string DireccionRequest { get; set; }

        public string DireccionToken { get; set; }

    }
}