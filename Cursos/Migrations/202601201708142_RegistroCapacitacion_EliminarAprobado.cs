namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RegistroCapacitacion_EliminarAprobado : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.RegistrosCapacitaciones", "Aprobado");
        }
        
        public override void Down()
        {
            AddColumn("dbo.RegistrosCapacitaciones", "Aprobado", c => c.Boolean(nullable: false));
        }
    }
}
