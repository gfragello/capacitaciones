using Cursos.Models;
using Cursos.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cursos.Helpers
{
    public class RegistroCapacitacionHelper
    {
        private readonly static RegistroCapacitacionHelper _instance = new RegistroCapacitacionHelper();

        public static RegistroCapacitacionHelper GetInstance()
        {
            return _instance;
        }

        public string ObtenerLabelEstado(RegistroCapacitacion r)
        {
            return ObtenerLabelEstado(r.Estado);
        }

        public string ObtenerLabelEstado(EstadosRegistroCapacitacion estado)
        {
            string estadoTexto = estado.ToString();

            switch (estado)
            {
                case Models.Enums.EstadosRegistroCapacitacion.Aprobado:
                    return string.Format("<h4><span id=\"spanCantidadCuposDisponibles\" class=\"label label-success\">{0}</span></h4>", estadoTexto);

                case Models.Enums.EstadosRegistroCapacitacion.NoAprobado:
                    return string.Format("<h4><span id=\"spanCantidadCuposDisponibles\" class=\"label label-danger\">{0}</span></h4>", estadoTexto);

                default:
                    return string.Format("<h4><span id=\"spanCantidadCuposDisponibles\" class=\"label label-info\">{0}</span></h4>", estadoTexto);
            }
        }
    }
}