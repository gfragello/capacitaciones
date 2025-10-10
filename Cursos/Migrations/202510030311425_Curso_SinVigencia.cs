namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Curso_SinVigencia : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cursos", "SinVigencia", c => c.Boolean(nullable: false));
            AlterColumn("dbo.RegistrosCapacitaciones", "FechaVencimiento", c => c.DateTime());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.RegistrosCapacitaciones", "FechaVencimiento", c => c.DateTime(nullable: false));
            DropColumn("dbo.Cursos", "SinVigencia");
        }
    }
}
