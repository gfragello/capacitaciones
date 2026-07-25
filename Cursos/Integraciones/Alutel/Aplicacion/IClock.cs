using System;

namespace Cursos.Integraciones.Alutel.Aplicacion
{
    public interface IClock
    {
        DateTime UtcNow { get; }

        DateTime Now { get; }
    }
}
