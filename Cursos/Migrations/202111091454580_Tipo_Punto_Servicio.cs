namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Tipo_Punto_Servicio : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PuntosServicio", "Tipo", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PuntosServicio", "Tipo");
        }
    }
}
