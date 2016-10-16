namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Notas_Cursos : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cursos", "EvaluacionConNota", c => c.Boolean(nullable: false));
            AddColumn("dbo.Cursos", "PuntajeMaximo", c => c.Int(nullable: false));
            AddColumn("dbo.Cursos", "PuntajeMinimo", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cursos", "PuntajeMinimo");
            DropColumn("dbo.Cursos", "PuntajeMaximo");
            DropColumn("dbo.Cursos", "EvaluacionConNota");
        }
    }
}
