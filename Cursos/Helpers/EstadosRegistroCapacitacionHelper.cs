using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cursos.Models.Enums;

namespace Cursos.Helpers
{
    public sealed class EstadosRegistroCapacitacionHelper
    {
        private readonly static EstadosRegistroCapacitacionHelper _instance = new EstadosRegistroCapacitacionHelper();

        public static EstadosRegistroCapacitacionHelper GetInstance()
        {
            return _instance;
        }

        public string ObtenerLabelClassEstado(EstadosRegistroCapacitacion e)
        {
            switch (e)
            {
                case EstadosRegistroCapacitacion.Aprobado:
                    return "label-success";

                case EstadosRegistroCapacitacion.Inscripto:
                    return "label-primary";

                case EstadosRegistroCapacitacion.NoAprobado:
                    return "label-danger";

                default:
                    return "label-primary";
            }
        }
    }
}