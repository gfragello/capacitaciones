namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Cursos.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<Cursos.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "Cursos.Models.ApplicationDbContext";
        }

        protected override void Seed(Cursos.Models.ApplicationDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method
            //  to avoid creating duplicate seed data.

            // Seed RegimenesPago iniciales - Asegurarse que existen antes de cualquier otra operación
            context.RegimenesPago.AddOrUpdate(
                r => r.Nombre,
                new RegimenPago 
                { 
                    RegimenPagoID = 1, 
                    Nombre = "Estándar", 
                    Descripcion = "Pago dentro de los 30 días de la factura" 
                },
                new RegimenPago 
                { 
                    RegimenPagoID = 2, 
                    Nombre = "Anticipado", 
                    Descripcion = "Requiere pago por adelantado antes de la capacitación" 
                }
            );
            
            // Guardar cambios para asegurar que los regímenes existen antes de actualizar las empresas
            context.SaveChanges();
            
            // Asignar Régimen Estándar a cualquier empresa que no tenga un régimen asignado
            var empresasSinRegimen = context.Empresas.Where(e => e.RegimenPagoID == 0).ToList();
            foreach (var empresa in empresasSinRegimen)
            {
                empresa.RegimenPagoID = 1; // Asignar Régimen Estándar
            }
            context.SaveChanges();
        }
    }
}
