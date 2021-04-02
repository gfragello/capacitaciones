using Cursos.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Cursos.Helpers
{
    public class CapacitadoHelper
    {
        private readonly static CapacitadoHelper _instance = new CapacitadoHelper();

        public static CapacitadoHelper GetInstance()
        {
            return _instance;
        }

        public bool CambiarExtensionFotoAJPG(Capacitado c, CursosDbContext dbContext)
        {
            //var capacitado = db.Capacitados.Where(c => c.CapacitadoID == CapacitadoID).;
            var pathArchivo = dbContext.PathArchivos.Find(c.PathArchivoID);

            if (c.PathArchivo != null && c.PathArchivo.NombreArchivo.ToLower().Contains(".jpeg"))
            {
                string carpetaArchivo = PathArchivoHelper.GetInstance().ObtenerCarpetaFotoCapacitado(c.CapacitadoID);
                string pathDirectorio = Path.Combine(HttpContext.Current.Server.MapPath("~/Images/FotosCapacitados/"), carpetaArchivo);

                var pathArchivoImagenAnterior = Path.Combine(pathDirectorio, c.PathArchivo.NombreArchivo);
                var pathArchivoImagenNuevo = Path.Combine(pathDirectorio, c.PathArchivo.NombreArchivo.ToLower().Replace(".jpeg", ".jpg"));

                System.IO.File.Move(pathArchivoImagenAnterior, pathArchivoImagenNuevo);

                string nuevoNombreArchivo = c.PathArchivo.NombreArchivo.ToLower().Replace(".jpeg", ".jpg");
                //c.PathArchivo.NombreArchivo = nuevoNombreArchivo;

                pathArchivo.NombreArchivo = nuevoNombreArchivo;
                dbContext.SaveChanges();
            }
            else
                return false;

            return true;
        }
    }
}