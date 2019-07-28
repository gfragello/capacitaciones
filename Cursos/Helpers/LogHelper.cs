using Cursos.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Cursos.Helpers
{
    public sealed class LogHelper
    {
        private readonly static LogHelper _instance = new LogHelper();

        public static LogHelper GetInstance()
        {
            return _instance;
        }

        public void WriteMessage(string module, string message)
        {
            string nombreArchivo = string.Format("{0}{1}{2}_{3}.txt", DateTime.Now.Year.ToString(),
                                                                      DateTime.Now.Month.ToString().PadLeft(2, '0'),
                                                                      DateTime.Now.Day.ToString().PadLeft(2, '0'),
                                                                      module);

            string pathCompleto = HttpContext.Current.Server.MapPath(string.Format("~/logs/{0}/{1}", module, nombreArchivo));

            // Write the string to a file.
            System.IO.StreamWriter file = new System.IO.StreamWriter(pathCompleto, true);
                
            message = string.Format("{0} - {1}", DateTime.Now.ToLongTimeString(), message);

            file.WriteLine(message);

            file.Close();
        }

        public List<ArchivoLog> GetModuleLogFiles(string module)
        {
            List<ArchivoLog> listArchivosLogRet = new List<ArchivoLog>();

            string pathDirectorio = HttpContext.Current.Server.MapPath(string.Format("~/logs/{0}/", module));

            if (Directory.Exists(pathDirectorio))
            {
                foreach (var pathArchivo in Directory.GetFiles(pathDirectorio))
                {
                    string nombre = pathArchivo.Substring(pathArchivo.LastIndexOf('\\') + 1);

                    ArchivoLog a = new ArchivoLog
                    {
                        Nombre = nombre,
                        Path = string.Format("~/logs/{0}/{1}", module, nombre)
                    };

                    listArchivosLogRet.Add(a);
                }

                listArchivosLogRet.Reverse();
            }

            return listArchivosLogRet;
        }
    }
}