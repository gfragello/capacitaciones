namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Curso_MostrarEnIndex_Activo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cursos", "MostrarEnIndexCapacitado", c => c.Boolean(nullable: false));
            AddColumn("dbo.Cursos", "Activo", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cursos", "Activo");
            DropColumn("dbo.Cursos", "MostrarEnIndexCapacitado");
        }
    }
}
