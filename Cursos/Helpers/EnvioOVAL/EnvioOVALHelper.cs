using Cursos.Models;
using Cursos.Models.Enums;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Web;

namespace Cursos.Helpers.EnvioOVAL
{
    public class EnvioOVALHelper
    {
        private readonly static EnvioOVALHelper _instance = new EnvioOVALHelper();

        public static EnvioOVALHelper GetInstance()
        {
            return _instance;
        }

        private CursosDbContext db = new CursosDbContext();

        public RespuestaOVAL EnviarDatosRegistro(RegistroCapacitacion r)
        {
            const string module = "enviosOVAL";

            DateTime fechaJornada = r.Jornada.Fecha;
            ServiceEnviarDatosOVAL.WebServiceResponse  rOVAL;

            try
            {
                string mensajelog = string.Format("Iniciando envío de datos\r\n\t{0}\r\n\t{1}\r\n\t{2}\r\n\t{3}",
                                               r.Capacitado.DocumentoCompleto,
                                               r.Capacitado.NombreCompleto,
                                               r.Jornada.JornadaIdentificacionCompleta,
                                               r.Estado.ToString());

                LogHelper.GetInstance().WriteMessage(module, mensajelog);

                string direccionServicioEnviarDatosOVAL = ConfiguracionHelper.GetInstance().GetValue("Direccion", "EnvioOVAL");

                LogHelper.GetInstance().WriteMessage(module, string.Format("Conectándose al servicio ubicado en {0}", direccionServicioEnviarDatosOVAL));

                BasicHttpsBinding binding = new BasicHttpsBinding();
                EndpointAddress address = new EndpointAddress(direccionServicioEnviarDatosOVAL);

                ServiceEnviarDatosOVAL.ServiceSoapClient sOVAL = new ServiceEnviarDatosOVAL.ServiceSoapClient(binding, address);

                rOVAL = sOVAL.get_induccion(r.Capacitado.TipoDocumento.TipoDocumentoOVAL,
                                            r.Capacitado.Documento,
                                            "CAP_SEG",
                                            r.Estado == EstadosRegistroCapacitacion.Aprobado ? "APR" : "REC",
                                            string.Format("{0}-{1}-{2}", fechaJornada.Day.ToString().PadLeft(2, '0'), fechaJornada.Month.ToString().PadLeft(2, '0'), fechaJornada.Year.ToString()),
                                            string.Empty);

                LogHelper.GetInstance().WriteMessage(module, string.Format("Conexión finalizada\r\n\tResult: {0}\r\n\tErrorMessage: {1}", rOVAL.Result, rOVAL.ErrorMessage));

                //si la propiedad Result tiene contenido, el registro se recibió correctamente
                if (rOVAL.Result != string.Empty)
                {
                    LogHelper.GetInstance().WriteMessage(module, "El registro fue recibido por el sistema OVAL");
                    return new RespuestaOVAL() { Codigo = 0, Mensaje = rOVAL.Result };
                }
                else
                {
                    LogHelper.GetInstance().WriteMessage(module, string.Format("El registro fue rechazado por el sistema OVAL ({0})", rOVAL.ErrorMessage));
                    return new RespuestaOVAL() { Codigo = 1, Mensaje = rOVAL.ErrorMessage };
                }
            }
            catch (Exception e)
            {
                LogHelper.GetInstance().WriteMessage(module, e.Message);
                return new RespuestaOVAL() { Codigo = -1, Mensaje = e.Message };
            }

            /*
            const string module = "enviosOVAL";

            string mensajelog = string.Format("Iniciando envío de datos\r\n\t\t\t{0}\r\n\t\t\t{1}\r\n\t\t\t{2}\r\n\t\t\t{3}",
                                               r.Capacitado.DocumentoCompleto,
                                               r.Capacitado.NombreCompleto,
                                               r.Jornada.JornadaIdentificacionCompleta,
                                               r.Estado.ToString());

            LogHelper.GetInstance().WriteMessage(module, mensajelog);

            try
            {
                var client = new RestClient(ConfiguracionHelper.GetInstance().GetValue("Direccion", "EnvioOVAL"));
                // client.Authenticator = new HttpBasicAuthenticator(username, password);

                //var request = new RestRequest("resource/{id}", Method.POST);
                //request.AddParameter("name", "value"); // adds to POST or URL querystring based on Method
                //request.AddUrlSegment("id", "123"); // replaces matching token in request.Resource

                var request = new RestRequest(method: Method.POST);
                request.AddParameter("TipoDocumento", "CI");
                request.AddParameter("NumeroDocumento", r.Capacitado.Documento);
                request.AddParameter("TipoInduccion", "IND");
                request.AddParameter("Aprobado", "true");
                request.AddParameter("FechaInduccion", DateTime.Now);
                request.AddParameter("Notas", "");

                LogHelper.GetInstance().WriteMessage(module, "Iniciando envío de datos");

                // execute the request
                IRestResponse response = client.Execute(request);
                var content = response.Content; // raw content as string

                mensajelog = string.Format("Envío de datos completado.\r\n\t\t\tRespuesta recibida:{0}", content);

                LogHelper.GetInstance().WriteMessage(module, mensajelog);

                IRestResponse<RespuestaOVAL> respuesta = client.Execute<RespuestaOVAL>(request);

                return (RespuestaOVAL)respuesta.Data;
            }
            catch (Exception e)
            {
                mensajelog = string.Format("ERROR - {0}", e.Message);
            }
            */

            return null;
        }

        public bool EnviarDatosListaRegistros(List<int> registrosCapacitacionIds, ref int totalAceptados, ref int totalRechazados)
        {
            foreach (var registroCapacitacionId in registrosCapacitacionIds)
            {
                if (!EnviarDatosRegistroOVAL(registroCapacitacionId, ref totalAceptados, ref totalRechazados))
                    return false;
            }

            return true;
        }

        public bool EnviarDatosRegistroOVAL(int registroCapacitacionId, ref int totalAceptados, ref int totalRechazados)
        {
            //se inicializa nuevamente el db context para evitar que se lean datos cacheados
            db = new CursosDbContext();
            var registroCapacitacion = db.RegistroCapacitacion.Where(r => r.RegistroCapacitacionID == registroCapacitacionId).FirstOrDefault();

            if (registroCapacitacion != null)
            {
                if (registroCapacitacion.ListoParaEnviarOVAL)
                {
                    EstadosEnvioOVAL estado;
                    RespuestaOVAL respuesta = this.EnviarDatosRegistro(registroCapacitacion);

                    switch (respuesta.Codigo)
                    {
                        case 0: //si el registro se recibió correctamente

                            estado = EstadosEnvioOVAL.Aceptado;
                            totalAceptados++;

                            break;

                        case 1: //si el registro no se recibió correctamente

                            estado = EstadosEnvioOVAL.Rechazado;
                            totalRechazados++;

                            break;

                        default:

                            return false;
                    }

                    registroCapacitacion.EnvioOVALEstado = estado;
                    registroCapacitacion.EnvioOVALFechaHora = DateTime.Now;
                    registroCapacitacion.EnvioOVALUsuario = HttpContext.Current.User.Identity.Name;
                    registroCapacitacion.EnvioOVALMensaje = respuesta.Mensaje;

                    db.Entry(registroCapacitacion).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

            return true;
        }
    }
}