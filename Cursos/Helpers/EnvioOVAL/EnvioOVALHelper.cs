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

            // execute the request
            IRestResponse response = client.Execute(request);
            var content = response.Content; // raw content as string

            IRestResponse<RespuestaOVAL> respuesta = client.Execute<RespuestaOVAL>(request);

            return (RespuestaOVAL)respuesta.Data;
        }

        public bool EnviarDatosListaRegistros(List<int> registrosCapacitacionIds, out int totalAceptados, out int totalRechazados)
        {
            totalAceptados = 0;
            totalRechazados = 0;

            foreach (var registroCapacitacionId in registrosCapacitacionIds)
            {
                EnviarDatosRegistroOVAL(registroCapacitacionId, out totalAceptados, out totalRechazados);
            }

            return true;
        }

        public bool EnviarDatosRegistroOVAL(int registroCapacitacionId, out int totalAceptados, out int totalRechazados)
        {
            totalAceptados = 0;
            totalRechazados = 0;

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