using System.ComponentModel.DataAnnotations;

namespace Cursos.Models.Enums
{
    public enum TiposArchivo
    {
        [Display(Name = "Foto del Capacitado")]
        FotoCapacitado = 0, //valor por defecto
        ActaEscaneada
    }
}