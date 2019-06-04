using Cursos.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cursos.Helpers
{
    public class JornadaHelper
    {
        private readonly static JornadaHelper _instance = new JornadaHelper();

        public static JornadaHelper GetInstance()
        {
            return _instance;
        }

        public string ObtenerLabelTotalCuposDisponibles(Jornada j)
        {
            string cantidadCuposDisponiblesTexto = j.CantidadCuposDisponiblesTexto;

            if (j.QuedanCuposDisponibles)
                return string.Format("<h4><span id=\"spanCantidadCuposDisponibles\" class=\"label label-success\">{0}</span></h4>", cantidadCuposDisponiblesTexto);
            else
                return string.Format("<h4><span id=\"spanCantidadCuposDisponibles\" class=\"label label-danger\">{0}</span></h4>", cantidadCuposDisponiblesTexto);
        }

        public string ObtenerLabelTotalInscriptos(Jornada j)
        {
            return string.Format("<h4><span class=\"label label-default\">{0}</span></h4>", j.TotalInscriptosTexto);
        }

        public string ObtenerLabelCierreInscripcion(Jornada j)
        {
            string cierreInscripcionTexto = j.CierreIncripcionTexto;

            if (j.TieneCierreIncripcion)
            {
                if (j.FechaCierreInscripcion >= DateTime.Now)
                    return string.Format("<h4><span id=\"spanCantidadCuposDisponibles\" class=\"label label-success\">{0}</span></h4>", cierreInscripcionTexto);
                else
                    return string.Format("<h4><span id=\"spanCantidadCuposDisponibles\" class=\"label label-danger\">{0}</span></h4>", cierreInscripcionTexto);
            }
            else
                return string.Format("<h4><span id=\"spanCantidadCuposDisponibles\" class=\"label label-success\">{0}</span></h4>", cierreInscripcionTexto);
        }
    }
}