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
    public class ArchivosFSHelper
    {
        //se implementa el patron singleton
        private readonly static ArchivosFSHelper _instance = new ArchivosFSHelper();

        private CursosDbContext db = new CursosDbContext();

        public static ArchivosFSHelper GetInstance()
        {
            return _instance;
        }

        public void RecorrerArchivosHuerfanos(List<string> subdirectorios)
        {
            //se recorre cada subdirectorio
            foreach (var subdirectorio in subdirectorios)
            {
                //se obtienen todos los archivos de tipo FotoCapacitado que no están asociados a ningún capacitado
                var archivosHuerfanos = db.PathArchivos.Where(pa => pa.TipoArchivo == TiposArchivo.FotoCapacitado && !db.Capacitados.Any(c => c.PathArchivoID == pa.PathArchivoId) && pa.SubDirectorio == subdirectorio).ToList();

                //se recorren los archivos y se ponene los id separados por coma en un archivo de texto que en el nombre contenga la fecha y hora actual
                string pathArchivo = HttpContext.Current.Server.MapPath("~/ArchivosHuerfanos_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
                System.IO.File.WriteAllText(pathArchivo, string.Join(",", archivosHuerfanos.Select(a => a.PathArchivoId)));

                foreach (var archivo in archivosHuerfanos)
                {
                    //se obtiene el path completo del archivo
                    string pathCompleto = Path.Combine(archivo.SubDirectorio.ToString(), archivo.NombreArchivo.ToString());

                    //se mueve el archivo a la carpeta de archivos huerfanos, dentro de una subcarpeta con el nombre depurados_yyyyMMddHHmmss
                    string pathDestino = HttpContext.Current.Server.MapPath("~/ArchivosHuerfanos/depurados_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "/");
                    System.IO.File.Move(pathCompleto, pathDestino);

                    //se elimina el registro de la base de datos
                    db.PathArchivos.Remove(archivo);
                    db.SaveChanges();
                }
            }


        }
        /*
        public void RecorrerArchivosHuerfanos()
        {
            //se buscan todos los archivos de tipo FotoCapacitado que no están asociados a ningún capacitado
            var archivosHuerfanos = db.PathArchivos.Where(pa => pa.TipoArchivo == TiposArchivo.FotoCapacitado && !db.Capacitados.Any(c => c.PathArchivoID == pa.PathArchivoId)).ToList();

            //se recorren los archivos y se ponene los id separados por coma en un archivo de texto que en el nombre contenga la fecha y hora actual
            string nombreArchivo = ;
            string pathArchivo = HttpContext.Current.Server.MapPath("~/ArchivosHuerfanos_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
            System.IO.File.WriteAllText(pathArchivo, string.Join(",", archivosHuerfanos.Select(a => a.PathArchivoId)));


            foreach (var archivo in archivosHuerfanos)
            {
                //se obtiene el path completo del archivo
                string pathCompleto = Path.Combine(archivo.SubDirectorio, archivo.NombreArchivo);

                //se elimina el archivo
                System.IO.File.Delete(pathCompleto);

                //se elimina el registro de la base de datos
                db.PathArchivos.Remove(archivo);
                db.SaveChanges();
            }
        }
        */
    }
}
