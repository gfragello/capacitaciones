using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cursos.Helpers
{
    public class CSSHelper
    {
        private readonly static CSSHelper _instance = new CSSHelper();

        public static CSSHelper GetInstance()
        {
            return _instance;
        }

        public string ObtenerEstiloVisible(bool visible)
        {
            if (visible)
                return "display:normal";
            else
                return "display:none";
        }
    }
}