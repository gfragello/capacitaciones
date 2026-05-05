namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Curso_Caracteristicas : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cursos", "Caracteristicas", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cursos", "Caracteristicas");
        }
    }
}
