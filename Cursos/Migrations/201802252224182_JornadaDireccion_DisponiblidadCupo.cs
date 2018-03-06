namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class JornadaDireccion_DisponiblidadCupo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Jornadas", "Direccion", c => c.String());
            AddColumn("dbo.Jornadas", "CuposDisponibles", c => c.Boolean(nullable: false));
            AddColumn("dbo.Lugares", "DireccionHabitual", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Lugares", "DireccionHabitual");
            DropColumn("dbo.Jornadas", "CuposDisponibles");
            DropColumn("dbo.Jornadas", "Direccion");
        }
    }
}
