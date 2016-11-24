using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cursos.Helpers
{
    public sealed class PathArchivoHelper
    {
        private readonly static PathArchivoHelper _instance = new PathArchivoHelper();

        public static PathArchivoHelper GetInstance()
        {
            return _instance;
        }

        public string ObtenerCarpetaFotoCapacitado(int CapacitadoId)
        {
            if (CapacitadoId > 100)
            { 
                long CapacitadoIdLong = (long)CapacitadoId;
                long numeroDesglosado = 0;
                long carpetaLong = 0;

                long i = 10;

                while (CapacitadoId > i / 10)
                {
                    numeroDesglosado = CapacitadoIdLong % i - CapacitadoIdLong % (i / 10);

                    if (numeroDesglosado >= 100)
                        carpetaLong += numeroDesglosado;


                    i *= 10;
                }

                return carpetaLong.ToString().PadLeft(6, '0');
            }
            else
            {
                return "000000";
            }
        }
    }
}