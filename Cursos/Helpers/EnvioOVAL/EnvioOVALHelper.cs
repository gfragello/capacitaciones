﻿using Cursos.Models;
using Cursos.Models.Enums;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
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

        private ApplicationDbContext db = new ApplicationDbContext();

        public RespuestaOVAL EnviarDatosRegistro(RegistroCapacitacion r)
        {
            switch (r.Jornada.Curso.PuntoServicio.Tipo)
            {
                case TipoPuntoServicio.SOAP:
                    return EnviarDatosRegistroSOAP(r);

                case TipoPuntoServicio.Rest:
                    return EnviarDatosRegistroRest(r);
            }

            return new RespuestaOVAL() { Codigo = -1, Mensaje = "Tipo de Punto de Servicio NO VÁLIDO" };
        }

        private RespuestaOVAL EnviarDatosRegistroSOAP(RegistroCapacitacion r)
        {
            const string module = "enviosOVAL";

            DateTime fechaJornada = r.Jornada.Fecha;
            ServiceEnviarDatosFotoOVAL.WebServiceResponse rOVAL;

            try
            {
                string mensajelog = string.Format("Iniciando envío de datos\r\n\t{0}\r\n\t{1}\r\n\t{2}\r\n\t{3}",
                                               r.Capacitado.DocumentoCompleto,
                                               r.Capacitado.NombreCompleto,
                                               r.Jornada.JornadaIdentificacionCompleta,
                                               r.Estado.ToString());

                LogHelper.GetInstance().WriteMessage(module, mensajelog);

                string direccionServicioEnviarDatosOVAL = r.Jornada.Curso.PuntoServicio.Direccion;

                LogHelper.GetInstance().WriteMessage(module, string.Format("Conectándose al servicio ubicado en {0}", direccionServicioEnviarDatosOVAL));

                BasicHttpsBinding binding = new BasicHttpsBinding();
                EndpointAddress address = new EndpointAddress(direccionServicioEnviarDatosOVAL);

                ServiceEnviarDatosFotoOVAL.ServiceSoapClient sOVAL = new ServiceEnviarDatosFotoOVAL.ServiceSoapClient(binding, address);
                ServiceEnviarDatosFotoOVAL.TokenSucurity token = new ServiceEnviarDatosFotoOVAL.TokenSucurity
                {
                    Username = r.Jornada.Curso.PuntoServicio.Usuario,
                    Password = r.Jornada.Curso.PuntoServicio.Password
                };

                //Se invoca el método para validar las credenciaes del usuario (devuelve un string)
                string responseToken = sOVAL.AuthenticationUser(token);

                token.AuthenToken = responseToken;

                byte[] fotoCapacitadoAsArray = null;

                if (r.Capacitado.PathArchivo != null)
                {
                    if (r.Capacitado.PathArchivo.NombreArchivo.ToLower().Contains(".jpeg"))
                    {
                        if (bool.Parse(ConfiguracionHelper.GetInstance().GetValue("ForzarExtensionArchivoImagenJPG", "EnvioOVAL")))
                        {
                            if (CapacitadoHelper.GetInstance().CambiarExtensionFotoAJPG(r.Capacitado, this.db))
                            {
                                LogHelper.GetInstance().WriteMessage(module, string.Format("Se modificó la extensión del archivo {0}", r.Capacitado.PathArchivo.NombreArchivo));
                            }
                            else
                            {
                                LogHelper.GetInstance().WriteMessage(module, string.Format("No se pudo modificar la extensión del archivo {0}", r.Capacitado.PathArchivo.NombreArchivo));
                            }
                        }
                        else
                        {
                            LogHelper.GetInstance().WriteMessage(module, string.Format("No se modificará la extensión del archivo {0} porque está deshabilitada por configuración", r.Capacitado.PathArchivo.NombreArchivo));
                        }
                    }

                    //se obtiene el path donde está almacenada la imagen que se enviará al web service
                    string carpetaArchivo = PathArchivoHelper.GetInstance().ObtenerCarpetaFotoCapacitado(r.Capacitado.CapacitadoID);
                    string pathDirectorio = Path.Combine(HttpContext.Current.Server.MapPath("~/Images/FotosCapacitados/"), carpetaArchivo);

                    var pathArchivoImagen = Path.Combine(pathDirectorio, r.Capacitado.PathArchivo.NombreArchivo);

                    fotoCapacitadoAsArray = this.GetImageAsByteArray(pathArchivoImagen);
                }

                rOVAL = sOVAL.get_induccion(token,
                                            r.Capacitado.TipoDocumento.TipoDocumentoOVAL,
                                            r.Capacitado.Documento,
                                            r.Capacitado.Nombre,
                                            r.Capacitado.Apellido,
                                            string.Empty,
                                            r.Capacitado.Empresa.RUT,
                                            r.Capacitado.Empresa.NombreFantasia,
                                            "CAP_SEG",
                                            r.Estado == EstadosRegistroCapacitacion.Aprobado ? "APR" : "REC",
                                            string.Format("{0}-{1}-{2}", fechaJornada.Day.ToString().PadLeft(2, '0'), fechaJornada.Month.ToString().PadLeft(2, '0'), fechaJornada.Year.ToString()),
                                            string.Empty,
                                            fotoCapacitadoAsArray);

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
        }

        private RespuestaOVAL EnviarDatosRegistroRest(RegistroCapacitacion r)
        {
            const string module = "enviosOVAL";

            DateTime fechaJornada = r.Jornada.Fecha;

            string fotoCapacitadoAsBase64 = String.Empty;

            string mensajelog = string.Format("Iniciando envío de datos\r\n\t{0}\r\n\t{1}\r\n\t{2}\r\n\t{3}",
                                               r.Capacitado.DocumentoCompleto,
                                               r.Capacitado.NombreCompleto,
                                               r.Jornada.JornadaIdentificacionCompleta,
                                               r.Estado.ToString());

            LogHelper.GetInstance().WriteMessage(module, mensajelog);

            if (r.Capacitado.PathArchivo != null)
            {
                //se obtiene el path donde está almacenada la imagen que se enviará al web service
                string carpetaArchivo = PathArchivoHelper.GetInstance().ObtenerCarpetaFotoCapacitado(r.Capacitado.CapacitadoID);
                string pathDirectorio = Path.Combine(HttpContext.Current.Server.MapPath("~/Images/FotosCapacitados/"), carpetaArchivo);

                var pathArchivoImagen = Path.Combine(pathDirectorio, r.Capacitado.PathArchivo.NombreArchivo);

                //byte[] fotoCapacitadoAsArray = null;
                byte[] fotoCapacitadoAsArray = this.GetImageAsByteArray(pathArchivoImagen);
                fotoCapacitadoAsBase64 = Convert.ToBase64String(fotoCapacitadoAsArray); 
            }

            PuntoServicio puntoServicioRest = r.Jornada.Curso.PuntoServicio;

            LogHelper.GetInstance().WriteMessage(module, string.Format("Punto servicio: {0}", puntoServicioRest.Nombre));

            var client = new RestClient(puntoServicioRest.Direccion);
            var request = new RestRequest(puntoServicioRest.DireccionRequest, Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            LogHelper.GetInstance().WriteMessage(module, "Obteniendo datos de seguridad");
            DatosTokenSeguridadOVAL datosTokenOAuthOVAL = ObtenerTokenOAuthOVAL(puntoServicioRest);

            if (datosTokenOAuthOVAL != null)
            {
                LogHelper.GetInstance().WriteMessage(module, "Datos de seguridad obtenidos correctamente");

                request.AddHeader("cliente", datosTokenOAuthOVAL.cliente);
                request.AddHeader("api_key", datosTokenOAuthOVAL.api_key);

                request.AddHeader("Content-type", "application/json");

                request.AddJsonBody(
                    new
                    {
                        tipo_doc = r.Capacitado.TipoDocumento.TipoDocumentoOVAL,
                        rut_trabajador = r.Capacitado.Documento,
                        nombres_trabajador = r.Capacitado.Nombre,
                        ape_paterno_trabajador = r.Capacitado.Apellido,
                        ape_materno_trabajador = string.Empty,
                        rut_contratista = r.Capacitado.Empresa.RUT,
                        nombre_contratista = r.Capacitado.Empresa.NombreFantasia,
                        tipo_induccion = "CAP_SEG",
                        estado_induccion = r.Estado == EstadosRegistroCapacitacion.Aprobado ? "APR" : "REC",
                        fecha_induccion = string.Format("{0}-{1}-{2}", fechaJornada.Day.ToString().PadLeft(2, '0'), fechaJornada.Month.ToString().PadLeft(2, '0'), fechaJornada.Year.ToString()),
                        imagen = fotoCapacitadoAsBase64
                    });

                LogHelper.GetInstance().WriteMessage(module, string.Format("Ejecutando el envío de datos a la siguiente dirección: {0}/{1}", puntoServicioRest.Direccion, puntoServicioRest.DireccionRequest));
                var tResponse = client.Execute(request);

                if (tResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    LogHelper.GetInstance().WriteMessage(module, "El servicio fue invocado correctamente. Evaluando la respuesta recibida.");
                    LogHelper.GetInstance().WriteMessage(module, string.Format("Respuesta recibida (RAW): {0}", tResponse.Content));

                    string respuestaServicio = ObtenerRespuestaServicioREST(tResponse.Content);
                    int codigoRet = 0; //0 es el código correspondiente a envío exitoso

                    if (respuestaServicio != string.Empty)
                    {
                        LogHelper.GetInstance().WriteMessage(module, string.Format("Ocurrió el siguiente error en el envío de datos: {0}", respuestaServicio));
                        codigoRet = 1; //indica que hubo un error al invocar el servicio
                    }

                    return new RespuestaOVAL() { Codigo = codigoRet, Mensaje = respuestaServicio };
                }
                else
                {
                    LogHelper.GetInstance().WriteMessage(module, "Error al invocar el servicio");
                    return new RespuestaOVAL() { Codigo = -1, Mensaje = "Error al invocar el servicio" };
                }
            }
            else
            {
                LogHelper.GetInstance().WriteMessage(module, "Error al invocar el servicio de autenticación (token)");
                return new RespuestaOVAL() { Codigo = -1, Mensaje = "Error al invocar el servicio de autenticación (token)" };
            }
        }

        private DatosTokenSeguridadOVAL ObtenerTokenOAuthOVAL(PuntoServicio puntoServicioRest)
        {
            DatosTokenSeguridadOVAL ret = null;

            string url = puntoServicioRest.DireccionToken;
            string usuario = puntoServicioRest.Usuario;
            string password = puntoServicioRest.Password;

            //request token
            var restclient = new RestClient(url);
            RestRequest request = new RestRequest() { Method = Method.POST };
            request.AddHeader("accept", "application/json");
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("user", usuario);
            request.AddParameter("passwd", password);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var tResponse = restclient.Execute(request);

            if (tResponse.IsSuccessful)
            {
                var responseJson = tResponse.Content;
                var status = bool.Parse(JsonConvert.DeserializeObject<Dictionary<string, object>>(responseJson)["status"].ToString());

                if (status == true)
                {
                    ret = new DatosTokenSeguridadOVAL
                    {
                        cliente = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseJson)["cliente"].ToString(),
                        api_key = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseJson)["api_key"].ToString()
                    };
                }

                return ret;
            }
            else
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
            db = new ApplicationDbContext();
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

        public byte[] GetImageAsByteArray(string pathFile)
        {
            Image img = Image.FromFile(pathFile);
            byte[] arr;

            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                arr = ms.ToArray();
            }

            //TODO 20210606 - Cuando se usa el archivo queda lockeado. Cambiar esta función
            //https://stackoverflow.com/questions/6576341/open-image-from-file-then-release-lock

            return arr;
        }

        private string ObtenerRespuestaServicioREST(string responseJson)
        {
            string respuestaServicio = string.Empty;
            
            var nombreCampo = responseJson.Contains("data") ? "data": "status";
            var envioCorrecto = bool.Parse(JsonConvert.DeserializeObject<Dictionary<string, object>>(responseJson)[nombreCampo].ToString());

            if (!envioCorrecto)
            {
                respuestaServicio = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseJson)["error"].ToString();
            }

            return respuestaServicio;
        }
    }
}