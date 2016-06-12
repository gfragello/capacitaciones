using Cursos.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    public abstract class ElementoAccesoControlado
    {
        public string UsuarioModificacion { get; private set; }
        public DateTime FechaModficacion { get; private set; }

        public void SetearAtributosControl()
        {
            this.UsuarioModificacion = HttpContext.Current.User.Identity.Name;
            this.FechaModficacion = DateTime.Now;
        }

        public bool PuedeModificarse()
        {
            if (HttpContext.Current.User.IsInRole("Administrador"))
            {
                //los usuarios con perfil Administrador pueden modificar incondicionalmente cualquier datos
                return true;
            }
            else if (HttpContext.Current.User.IsInRole("AdministradorExterno"))
            {
                //los usuarios con perfil AdministradorExterno pueden modificar solo los elementos que crearon
                if (this.UsuarioModificacion == HttpContext.Current.User.Identity.Name)
                    return true;
            }

            return false;
        }
    }
}