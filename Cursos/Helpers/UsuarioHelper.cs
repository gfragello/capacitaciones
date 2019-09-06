using Cursos.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cursos.Helpers
{
    public class UsuarioHelper
    {
        private static UsuarioHelper instance = null;

        private ApplicationDbContext db = new ApplicationDbContext();
        private CursosDbContext dbcursos = new CursosDbContext();

        private UsuarioHelper() { }

        public static UsuarioHelper GetInstance()
        {
            if (instance == null)
                instance = new UsuarioHelper();

            return instance;
        }

        public string ObtenerRoleName(string RoleId)
        {
            return db.Roles.Where(r => r.Id == RoleId).FirstOrDefault().Name;
        }

        public string ObtenerNombreEmpresaAsociada(string user)
        {
            var EmpresaUsuario = dbcursos.EmpresasUsuarios.Where(eu => eu.Usuario == user).FirstOrDefault();

            if (EmpresaUsuario != null)
                return dbcursos.Empresas.Find(EmpresaUsuario.EmpresaID).NombreFantasia;
            else
                return String.Empty;
        }

        public Empresa ObtenerEmpresaAsociada(string username)
        {
            var empresausuario = dbcursos.EmpresasUsuarios.Where(eu => eu.Usuario == username).FirstOrDefault();

            if (empresausuario != null)
                return empresausuario.Empresa;

            return empresausuario != null ? empresausuario.Empresa : null;
        }

        public bool UsuarioTieneRol(string username, string rolename)
        {
            var user = db.Users.Where(u => u.UserName == username).FirstOrDefault();

            if (user != null)
            {
                foreach (var ur in user.Roles)
                {
                    var role = db.Roles.Where(r => r.Id == ur.RoleId).FirstOrDefault();

                    if (role != null)
                        if (role.Name == rolename) return true;
                }
            }

            return false;
        }

    }
}