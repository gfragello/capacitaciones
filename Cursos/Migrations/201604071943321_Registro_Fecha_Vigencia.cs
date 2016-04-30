namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Registro_Fecha_Vigencia : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RegistrosCapacitaciones", "FechaVencimiento", c => c.DateTime(nullable: false));
            AddColumn("dbo.Cursos", "Vigencia", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cursos", "Vigencia");
            DropColumn("dbo.RegistrosCapacitaciones", "FechaVencimiento");
        }
    }
}
