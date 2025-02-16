namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Curso_EnviarActaEmail : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cursos", "EnviarActaEmail", c => c.Boolean(nullable: false));
            AddColumn("dbo.Cursos", "ActaEmail", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cursos", "ActaEmail");
            DropColumn("dbo.Cursos", "EnviarActaEmail");
        }
    }
}
