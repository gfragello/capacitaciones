using Cursos.Models;
using Cursos.Models.Enums;
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

        public bool EnviarDatos(List<RegistroCapacitacion> registrosCapacitacion, out int totalAceptados, out int totalRechazados)
        {
            totalAceptados = 0;
            totalRechazados = 0;

            EstadosEnvioOVAL estado;

            foreach (var rID in registrosCapacitacion)
            {
                var r = db.RegistroCapacitacion.Where(rc => rc.RegistroCapacitacionID == rID.RegistroCapacitacionID).FirstOrDefault();

                if (r.ListoParaEnviarOVAL)
                {
                    int primerDigito;

                    if (int.TryParse(r.Capacitado.Documento.Substring(0, 1), out primerDigito))
                        if (primerDigito % 2 == 0) //si es un número par, se considera incorrecto el envío
                            estado = EstadosEnvioOVAL.Rechazado;
                        else
                            estado = EstadosEnvioOVAL.Aceptado;
                    else //si el primer digito no es un número, se considera correcto el envío
                        estado = EstadosEnvioOVAL.Aceptado;

                    if (estado == EstadosEnvioOVAL.Aceptado) totalAceptados++;
                    if (estado == EstadosEnvioOVAL.Rechazado) totalRechazados++;

                    r.EnvioOVALEstado = estado;
                    r.EnvioOVALFechaHora = DateTime.Now;
                    r.EnvioOVALUsuario = HttpContext.Current.User.Identity.Name;

                    if (estado == EstadosEnvioOVAL.Rechazado)
                        r.EnvioOVALMensaje = "Se rechazó la rececpción del registro";

                    db.Entry(r).State = EntityState.Modified;
                    db.SaveChanges();

                    Thread.Sleep(1000);
                }
            }

            return true;
        }
    }
}