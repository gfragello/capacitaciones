using Cursos.Models;
using Cursos.Models.Enums;
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
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                var conf = db.Configuracion.Where(c => c.Index == Index && c.Seccion == Seccion).FirstOrDefault();

                if (conf != null)
                    return conf.Value;
            }

            return string.Empty;
        }

        public TipoAlmacenamiento GetCapacitado_TipoAlmacenamientoFotoDefecto()
        {
            string tipoAlmacenamiento = GetValue("TipoAlmacenamientoFotoDefecto", "Capacitados");

            if (tipoAlmacenamiento == "FileSystem")
                return TipoAlmacenamiento.FileSystem;
            else if (tipoAlmacenamiento == "BlobStorage")
                return TipoAlmacenamiento.BlobStorage;

            return TipoAlmacenamiento.FileSystem;
        }
    }
}