using Cursos.Models;
using Cursos.Models.Enums;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
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

                /*
                var request = new RestRequest("resource/{id}", Method.POST);
                request.AddParameter("name", "value"); // adds to POST or URL querystring based on Method
                request.AddUrlSegment("id", "123"); // replaces matching token in request.Resource
                */

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

            return null;
        }

        public bool EnviarDatosListaRegistros(List<int> registrosCapacitacionIds, ref int totalAceptados, ref int totalRechazados)
        {
            foreach (var registroCapacitacionId in registrosCapacitacionIds)
            {
                EnviarDatosRegistroOVAL(registroCapacitacionId, ref totalAceptados, ref totalRechazados);
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

                    //if (EnvioOVALHelper.GetInstance().EnviarDatosListaRegistros(registroCapacitacion, out totalAceptados, out totalRechazados))
                    if (respuesta.Codigo == 0) //si el registro se recibio corrrectamente
                    {
                        estado = EstadosEnvioOVAL.Aceptado;
                        totalAceptados++;
                    }
                    else
                    {
                        estado = EstadosEnvioOVAL.Rechazado;
                        totalRechazados++;
                    }

                    registroCapacitacion.EnvioOVALEstado = estado;
                    registroCapacitacion.EnvioOVALFechaHora = DateTime.Now;
                    registroCapacitacion.EnvioOVALUsuario = HttpContext.Current.User.Identity.Name;

                    if (estado == EstadosEnvioOVAL.Rechazado)
                        registroCapacitacion.EnvioOVALMensaje = respuesta.Mensaje;

                    db.Entry(registroCapacitacion).State = EntityState.Modified;
                    db.SaveChanges();

                    return true;
                }
            }

            return false;
        }
    }
}