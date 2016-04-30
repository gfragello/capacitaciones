namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Curso_Color : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cursos", "ColorDeFondo", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cursos", "ColorDeFondo");
        }
    }
}
