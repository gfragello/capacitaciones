namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Jornada_Hora : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Jornadas", "Hora", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Jornadas", "Hora");
        }
    }
}
