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
            string estadoTexto = string.Empty;

            if (estado == EstadosRegistroCapacitacion.NoAprobado)
                estadoTexto = "No aprobado";
            else
                estadoTexto = estado.ToString();

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

        public string ObtenerImagenEnvioOVALEstado(RegistroCapacitacion r)
        {
            return ObtenerImagenEnvioOVALEstado(r.EnvioOVALEstado, r.ListoParaEnviarOVAL, r.EnvioOVALFechaHora, r.EnvioOVALMensaje, r.EnvioOVALUsuario);
        }

        private string ObtenerImagenEnvioOVALEstado(EstadosEnvioOVAL estado , bool listoParaEnviar, DateTime? envioOVALFechaHora, string envioOVALMensaje, string envioOVALUsuario)
        {
            string srcImagen = string.Empty;

            switch (estado)
            {
                case EstadosEnvioOVAL.PendienteEnvio:

                    if (listoParaEnviar)
                    {
                        srcImagen = VirtualPathUtility.ToAbsolute("~/images/OVALPendiente.png");
                        return string.Format("<img src='{0}' title='Pendiente de envío' />", srcImagen);
                    }
                    else
                    {
                        srcImagen = VirtualPathUtility.ToAbsolute("~/images/OVALPendienteNoListo.png");
                        return string.Format("<img src='{0}' title='No está listo para enviarse' />", srcImagen);
                    }

                case EstadosEnvioOVAL.Aceptado:

                    srcImagen = VirtualPathUtility.ToAbsolute("~/images/OVALAceptado.png");
                    return string.Format("<img src='{0}' title='Envío aceptado por sistema OVAL\n{1}\n{2}' />", srcImagen, envioOVALFechaHora.ToString(), envioOVALUsuario);

                case EstadosEnvioOVAL.Rechazado:

                    srcImagen = VirtualPathUtility.ToAbsolute("~/images/OVALRechazado.png");
                    return string.Format("<img src='{0}' title='Envío rechazado por sistema OVAL\n{1}\n{2}\n{3}' />", srcImagen, envioOVALMensaje, envioOVALFechaHora.ToString(), envioOVALUsuario);

                default:
                    return "&nbsp;";
            }
        }
    }
}