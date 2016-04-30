using Cursos.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cursos.Helpers
{
    public class ImagenEstadoRegistroHelper
    {
        private static ImagenEstadoRegistroHelper instance = null;

        private ImagenEstadoRegistroHelper() { }

        public static ImagenEstadoRegistroHelper GetInstance()
        {
            if (instance == null)
                instance = new ImagenEstadoRegistroHelper();

            return instance;
        }

        public void ObtenerImagenYTitulo(RegistroCapacitacion r, ref string srcImagen, ref string titleTexto)
        {
            if (r.FechaVencimiento < DateTime.Now) //si el regsitro ya está vencido
            {
                srcImagen = "~/images/vencido_20x20.png";
                titleTexto = String.Format("Registro vencido el {0}", r.FechaVencimiento.ToShortDateString());
            }

            if (r.FechaVencimiento > DateTime.Now) //si el registro aún no se ha vencido
            {
                if (r.FechaVencimiento.AddMonths(-1) > DateTime.Now) //si aún sigue vigente por más de un mes
                {
                    srcImagen = "~/images/vigente_20x20.png";
                    titleTexto = String.Format("Registro vigente. Vence el {0}", r.FechaVencimiento.ToShortDateString());
                }
                else
                {
                    srcImagen = "~/images/porVencer_20x20.png";
                    titleTexto = String.Format("El registro vence el {0}", r.FechaVencimiento.ToShortDateString());
                }
            }

        }
    }
}