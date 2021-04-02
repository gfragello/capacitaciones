using Cursos.Models;
using Cursos.Models.Enums;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.IO;
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
            ServiceEnviarDatosOVAL.WebServiceResponse rOVAL;

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
                ServiceEnviarDatosOVAL.TokenSucurity token = new ServiceEnviarDatosOVAL.TokenSucurity
                {
                    Username = ConfiguracionHelper.GetInstance().GetValue("Usuario", "EnvioOVAL"),
                    Password = ConfiguracionHelper.GetInstance().GetValue("Password", "EnvioOVAL")
                };

                //Se invoca el método para validar las credenciaes del usuario (devuelve un string)
                string responseToken = sOVAL.AuthenticationUser(token);

                token.AuthenToken = responseToken;

                rOVAL = sOVAL.get_induccion(token,
                                            r.Capacitado.TipoDocumento.TipoDocumentoOVAL,
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
        }

        public RespuestaOVAL EnviarDatosRegistroConFoto(RegistroCapacitacion r)
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
                    RespuestaOVAL respuesta = this.EnviarDatosRegistroConFoto(registroCapacitacion);

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

            return arr;
        }
    }
}