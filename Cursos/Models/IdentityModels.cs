using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Cursos.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        [Display(Name = "Tiene pie de firma")]
        public bool HasSignatureFooter { get; set; }

        [Display(Name = "Pie de firma")]
        [AllowHtml] // Allows HTML content
        public string SignatureFooter { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext() : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configuración de Identity (asegúrate de llamar al base)
            base.OnModelCreating(modelBuilder);

            // Configuración de entidades del dominio (copiada de CursosDbContext)
            modelBuilder.Entity<Jornada>()
                .HasRequired(j => j.Lugar)
                .WithMany(l => l.Jornadas)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Jornada>()
                .HasRequired(j => j.Curso)
                .WithMany(c => c.Jornadas)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Jornada>()
                .HasRequired(j => j.Instructor)
                .WithMany(i => i.Jornadas)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Capacitado>()
                .HasRequired(c => c.Empresa)
                .WithMany(e => e.Capacitados)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Capacitado>()
                .HasRequired(c => c.TipoDocumento)
                .WithMany(t => t.Capacitados)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<RegistroCapacitacion>()
                .HasRequired(r => r.Jornada)
                .WithMany(j => j.RegistrosCapacitacion)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Capacitado>()
                .HasOptional(c => c.PathArchivo);

            modelBuilder.Entity<Jornada>()
                .HasOptional(j => j.PathArchivo);

            modelBuilder.Entity<JornadaActaEnviada>()
                .HasRequired(ja => ja.Jornada)
                .WithMany(j => j.JornadaActasEnviadas)
                .HasForeignKey(ja => ja.JornadaID)
                .WillCascadeOnDelete(true);

            // Índice NO único para Documento en Capacitado
            modelBuilder.Entity<Capacitado>()
                .Property(c => c.Documento)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute("IX_Capacitado_Documento") { IsUnique = false }
                    )
                );

            //configura la relación entre el Instructor y los cursos
            //modelbuilder.Entity<Instructor>().HasMany(i => i.Cursos).WithMany(c => c.Instructores).Map(ic => ic.MapLeftKey("IntructorId")
            //                                                                                                   .MapRightKey("CursoId")
        }

        // Propiedades del dominio (copiadas de CursosDbContext)
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
        public DbSet<InstructorUsuario> InstructoresUsuarios { get; set; }
        public DbSet<PathArchivo> PathArchivos { get; set; }
        public DbSet<NotificacionVencimiento> NotificacionVencimientos { get; set; }
        public DbSet<Configuracion> Configuracion { get; set; }
        public DbSet<PuntoServicio> PuntoServicio { get; set; }
        public DbSet<MensajeUsuario> MensajesUsuarios { get; set; }

        public DbSet<JornadaActaEnviada> JornadaActasEnviadas { get; set; }
    }
}