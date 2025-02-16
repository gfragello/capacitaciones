using System.Data.Entity;
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
        /*
        //Boolean property to indicate if the user has a footer signature
        public bool HasSignatureFooter { get; set; }

        // Property to store the footer signature in HTML format
        [AllowHtml] // Allows HTML content
        public string SignatureFooter { get; set; }
        */

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    /*
    public class ApplicationUserData : IdentityUser
    {
        // Property to store the footer signature in HTML format
        [AllowHtml] // Allows HTML content
        public string SignatureFooter { get; set; }

        // Boolean property to indicate if the user has a footer signature
        public bool HasSignatureFooter { get; set; }

        // Propiedad de navegación inversa para enlazar con ApplicationUser
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
    */

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        //public DbSet<ApplicationUserData> ApplicationUserData { get; set; }

        /*
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de la relación entre ApplicationUser y UserProfile
            modelBuilder.Entity<ApplicationUser>()
                .HasOptional(u => u.ApplicationUserData)
                .WithRequired(up => up.User)
                .WillCascadeOnDelete(true);
        }
        */

        /*
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapear DatosUsuario a la tabla AspNetUsers
            modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers");

            // Ensure the new properties are included in the model
            modelBuilder.Entity<ApplicationUser>().Property(u => u.SignatureFooter).IsOptional();
            modelBuilder.Entity<ApplicationUser>().Property(u => u.HasSignatureFooter).IsOptional();
        }
        */
    }
}