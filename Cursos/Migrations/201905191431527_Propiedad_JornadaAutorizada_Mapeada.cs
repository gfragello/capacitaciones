namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Propiedad_JornadaAutorizada_Mapeada : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Jornadas", "Autorizada", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Jornadas", "Autorizada");
        }
    }
}
