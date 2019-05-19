namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Jornadas_Autorizables : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Jornadas", "RequiereAutorizacion", c => c.Boolean(nullable: false));
            AddColumn("dbo.Jornadas", "UsuarioCreacion", c => c.String());
            AddColumn("dbo.Jornadas", "FechaCreacion", c => c.DateTime(nullable: false));
            AddColumn("dbo.Jornadas", "UsuarioAutorizacion", c => c.String());
            AddColumn("dbo.Jornadas", "FechaAutorizacion", c => c.DateTime(nullable: false));
            AddColumn("dbo.Cursos", "RequiereAutorizacion", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cursos", "RequiereAutorizacion");
            DropColumn("dbo.Jornadas", "FechaAutorizacion");
            DropColumn("dbo.Jornadas", "UsuarioAutorizacion");
            DropColumn("dbo.Jornadas", "FechaCreacion");
            DropColumn("dbo.Jornadas", "UsuarioCreacion");
            DropColumn("dbo.Jornadas", "RequiereAutorizacion");
        }
    }
}
