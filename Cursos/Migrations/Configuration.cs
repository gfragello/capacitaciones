namespace Cursos.Migrations
{
    using Models;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Cursos.Models.CursosDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "Cursos.Models.CursosDbContext";
        }

        protected override void Seed(Cursos.Models.CursosDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            context.Departamentos.AddOrUpdate(
              d => d.Nombre,
              new Departamento { Nombre = "No especificado" },
              new Departamento { Nombre = "Artigas" },
              new Departamento { Nombre = "Canelones" },
              new Departamento { Nombre = "Cerro Largo" },
              new Departamento { Nombre = "Colonia" },
              new Departamento { Nombre = "Durazno" },
              new Departamento { Nombre = "Flores" },
              new Departamento { Nombre = "Florida" },
              new Departamento { Nombre = "Lavalleja" },
              new Departamento { Nombre = "Maldonado" },
              new Departamento { Nombre = "Montevideo" },
              new Departamento { Nombre = "Paysandú" },
              new Departamento { Nombre = "Río Negro" },
              new Departamento { Nombre = "Rivera" },
              new Departamento { Nombre = "Rocha" },
              new Departamento { Nombre = "Salto" },
              new Departamento { Nombre = "San José" },
              new Departamento { Nombre = "Soriano" },
              new Departamento { Nombre = "Tacuarembó" },
              new Departamento { Nombre = "Treinta y Tres" }
            );

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
        }
    }
}
