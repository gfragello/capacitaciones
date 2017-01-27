using Cursos.Models;
using Cursos.Models.Enums;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
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

        public PathArchivo ObtenerPathArchivo(string nombreArchivo, string carpetaArchivo, string pathDirectorio, HttpPostedFileBase archivo, TiposArchivo tipoArchivo)
        {
            PathArchivo pathFotoCapacitado = null;

            pathFotoCapacitado = new PathArchivo
            {
                NombreArchivo = nombreArchivo,
                SubDirectorio = carpetaArchivo,
                TipoArchivo = tipoArchivo
            };

            var pathArchivo = Path.Combine(pathDirectorio, pathFotoCapacitado.NombreArchivo);

            if (!System.IO.Directory.Exists(pathDirectorio))
                System.IO.Directory.CreateDirectory(pathDirectorio);

            archivo.SaveAs(pathArchivo);

            return pathFotoCapacitado;
        }

        public string ObtenerNombreFotoCapacitado(int capacitadoId, string extesionArchivo)
        {
            return string.Format("Foto_{0}_{1}{2}",
                                 capacitadoId.ToString().Trim(),
                                 (new Random()).Next(1000, 9999).ToString().Trim(),
                                 extesionArchivo);
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