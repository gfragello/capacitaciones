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

        public string ObtenerLabelCurso(Jornada j)
        {
            return string.Format("<h5><span class=\"label label-default\" style=\"color: #333333; background-color: {0}\">{1}</span></h5>",
                                 j.Curso.ColorDeFondo, j.Curso.DescripcionMultiLanguage);
        }

        public string ObtenerLabelTotalCuposDisponibles(Jornada j)
        {
            string cantidadCuposDisponiblesTexto = j.CantidadCuposDisponiblesTexto;

            if (j.QuedanCuposDisponibles)
            { 
                if (j.AlertaCuposDisponibles)
                    return string.Format("<h5><span id=\"spanCantidadCuposDisponibles\" class=\"label label-warning\">{0}</span></h5>", cantidadCuposDisponiblesTexto);
                else
                    return string.Format("<h5><span id=\"spanCantidadCuposDisponibles\" class=\"label label-success\">{0}</span></h5>", cantidadCuposDisponiblesTexto);
            }
            else
                return string.Format("<h5><span id=\"spanCantidadCuposDisponibles\" class=\"label label-danger\">{0}</span></h5>", cantidadCuposDisponiblesTexto);
        }

        public string ObtenerLabelTotalInscriptos(Jornada j)
        {
            return string.Format("<h5><span class=\"label label-default\">{0}</span></h5>", j.TotalInscriptosTexto);
        }

        public string ObtenerLabelCierreInscripcion(Jornada j)
        {
            string cierreInscripcionTexto = j.CierreIncripcionTexto;

            if (j.TieneCierreIncripcion)
            {
                if (j.FechaCierreInscripcion >= DateTime.Now)
                    return string.Format("<h5><span id=\"spanCantidadCuposDisponibles\" class=\"label label-success\">{0}</span></h5>", cierreInscripcionTexto);
                else
                    return string.Format("<h5><span id=\"spanCantidadCuposDisponibles\" class=\"label label-danger\">{0}</span></h5>", cierreInscripcionTexto);
            }
            else
                return string.Format("<h5><span id=\"spanCantidadCuposDisponibles\" class=\"label label-success\">{0}</span></h5>", cierreInscripcionTexto);
        }

        public string ObtenerLabelMinimoAsistentes(Jornada j)
        {
            string minimoAsistentesTexto = j.MinimoAsistentesTexto;

            if (j.TieneMinimoAsistentes)
            {
                if (j.TotalInscriptos >= j.MinimoAsistentes)
                    return string.Format("<h5><span id=\"spanCantidadCuposDisponibles\" class=\"label label-success\">{0}</span></h5>", minimoAsistentesTexto);
                else
                    return string.Format("<h5><span id=\"spanCantidadCuposDisponibles\" class=\"label label-danger\">{0}</span></h5>", minimoAsistentesTexto);
            }
            else
                return string.Format("<h5><span id=\"spanCantidadCuposDisponibles\" class=\"label label-success\">{0}</span></h5>", minimoAsistentesTexto);
        }

        public string ObtenerLabelDocumentacionAdicional(Jornada j)
        {
            if (j.TotalInscriptos > 0)
                if (j.DocumentacionAdicionalCompleta)
                    return string.Format("<h5><span id=\"spanCantidadCuposDisponibles\" class=\"label label-success\">{0}</span></h5>", j.TotalDocumentacionAdicionalPresentadaTexto);
                else
                    return string.Format("<h5><span id=\"spanCantidadCuposDisponibles\" class=\"label label-danger\">{0}</span></h5>", j.TotalDocumentacionAdicionalPresentadaTexto);
            else
                if (j.Curso.RequiereDocumentacionAdicionalInscripcion)
                    return string.Format("<h5><span id=\"spanCantidadCuposDisponibles\" class=\"label label-info\">{0}</span></h5>", "Se requiere la presentación de documentación adicional");
                else if (j.Curso.RequiereDocumentacionAdicionalInscripcionObligatoria)
                    return string.Format("<h5><span id=\"spanCantidadCuposDisponibles\" class=\"label label-info\">{0}</span></h5>", "Se requiere la presentación de documentación adicional obligatoria");

            return string.Empty;
        }
    }
}