namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Curso_VigenciaHastaFinAnio : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cursos", "VigenciaHastaFinAnio", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cursos", "VigenciaHastaFinAnio");
        }
    }
}
