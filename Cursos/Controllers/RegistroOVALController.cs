using Cursos.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Cursos.Controllers
{
    public class RegistroOVALController : ApiController
    {
        public RespuestaOVAL ingresarRegistro(RegistroOVAL registro)
        {
            RespuestaOVAL respuestaOVALRet = null;
            bool registroAceptado = false;
            int primerDigito;

            if (int.TryParse(registro.NumeroDocumento.Substring(0, 1), out primerDigito))
                if (primerDigito % 2 == 0) //si es un número par, se considera incorrecto el envío
                    registroAceptado = false;
                else
                    registroAceptado = true;
            else //si el primer digito no es un número, se considera correcto el envío
                registroAceptado = true;

            if (registroAceptado)
            {
                respuestaOVALRet = new RespuestaOVAL
                {
                    Codigo =  0,
                    Mensaje = string.Empty
                };
            }
            else
                respuestaOVALRet = new RespuestaOVAL
                {
                    Codigo = 1,
                    Mensaje =  "El documento no se encuentra ingresado en el sistema"
                };

            return respuestaOVALRet;
        }
    }
}
