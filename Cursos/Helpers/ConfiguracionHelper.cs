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

        private CursosDbContext db = new CursosDbContext();

        public string GetValue(string Index)
        {
            var conf = db.Configuracion.Where(c => c.Index == Index).FirstOrDefault();

            if (conf != null)
                return conf.Value;

            return string.Empty;
        }
    }
}