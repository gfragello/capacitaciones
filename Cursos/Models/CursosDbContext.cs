using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    public class CursosDbContext : DbContext
    {
        /*
        public CursosDbContext() : base("DefaultConnection")
        {
        }
        */

        protected override void OnModelCreating(DbModelBuilder modelbuilder)
        {
            //se sobreescribe el método OnModelCreating y se agrega lo siguiente (fluent API) para evitar que se hagan cascade deletes
            modelbuilder.Entity<Jornada>().HasRequired(j => j.Lugar).WithMany(l => l.Jornadas).WillCascadeOnDelete(false);
            modelbuilder.Entity<Jornada>().HasRequired(j => j.Curso).WithMany(c => c.Jornadas).WillCascadeOnDelete(false);
            modelbuilder.Entity<Jornada>().HasRequired(j => j.Instructor).WithMany(i => i.Jornadas).WillCascadeOnDelete(false);

            modelbuilder.Entity<Capacitado>().HasRequired(c => c.Empresa).WithMany(e => e.Capacitados).WillCascadeOnDelete(false);
            modelbuilder.Entity<Capacitado>().HasRequired(c => c.TipoDocumento).WithMany(t => t.Capacitados).WillCascadeOnDelete(false);

            //al borrar una joranda, se borraran sus registros asociados
            modelbuilder.Entity<RegistroCapacitacion>().HasRequired(r => r.Jornada).WithMany(j => j.RegistrosCapacitacion).WillCascadeOnDelete(true);

            //configura la relación entre el Capacitado y el PathArchivo
            modelbuilder.Entity<Capacitado>().HasOptional(c => c.PathArchivo); //se marca la foto del capacitado como opcional

            //configura la relación entre el Capacitado y el PathArchivo
            modelbuilder.Entity<Jornada>().HasOptional(j => j.PathArchivo); //se marca el acta como opcional

            base.OnModelCreating(modelbuilder);
        }

        public DbSet<Lugar> Lugares { get; set; }

        public DbSet<Instructor> Instructores { get; set; }

        public DbSet<Empresa> Empresas { get; set; }

        public DbSet<Curso> Cursos { get; set; }

        public DbSet<Jornada> Jornada { get; set; }

        public DbSet<Capacitado> Capacitados { get; set; }

        public DbSet<RegistroCapacitacion> RegistroCapacitacion { get; set; }

        public DbSet<TipoDocumento> TiposDocumento { get; set; }

        public DbSet<Departamento> Departamentos { get; set; }

        public DbSet<EmpresaUsuario> EmpresasUsuarios { get; set; }

        public DbSet<PathArchivo> PathArchivos { get; set; }
    }
}