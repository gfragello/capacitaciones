using Cursos.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cursos.Helpers
{
    public sealed class ConfiguracionHelper
    {
        private readonly static ConfiguracionHelper _instance = new ConfiguracionHelper();

        public static ConfiguracionHelper GetInstance()
        {
            return _instance;
        }

        public string GetValue(string Index, string Seccion)
        {
            using (CursosDbContext db = new CursosDbContext())
            {
                var conf = db.Configuracion.Where(c => c.Index == Index && c.Seccion == Seccion).FirstOrDefault();

                if (conf != null)
                    return conf.Value;
            }

            return string.Empty;
        }
    }
}