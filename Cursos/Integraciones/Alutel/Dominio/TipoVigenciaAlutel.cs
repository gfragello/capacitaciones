using System.ComponentModel.DataAnnotations;

namespace Cursos.Integraciones.Alutel.Dominio
{
    public enum TipoVigenciaAlutel
    {
        [Display(Name = "Tarjeta Verde")]
        TarjetaVerde = 1,

        [Display(Name = "Tarjeta Azul")]
        TarjetaAzul = 2,

        [Display(Name = "Refresh")]
        Refresh = 3
    }
}
