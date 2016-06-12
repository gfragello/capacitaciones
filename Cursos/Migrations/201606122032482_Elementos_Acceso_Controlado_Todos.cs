namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Elementos_Acceso_Controlado_Todos : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RegistrosCapacitaciones", "UsuarioModificacion", c => c.String());
            AddColumn("dbo.RegistrosCapacitaciones", "FechaModficacion", c => c.DateTime(nullable: false));
            AddColumn("dbo.Jornadas", "UsuarioModificacion", c => c.String());
            AddColumn("dbo.Jornadas", "FechaModficacion", c => c.DateTime(nullable: false));
            AddColumn("dbo.Instructores", "UsuarioModificacion", c => c.String());
            AddColumn("dbo.Instructores", "FechaModficacion", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Instructores", "FechaModficacion");
            DropColumn("dbo.Instructores", "UsuarioModificacion");
            DropColumn("dbo.Jornadas", "FechaModficacion");
            DropColumn("dbo.Jornadas", "UsuarioModificacion");
            DropColumn("dbo.RegistrosCapacitaciones", "FechaModficacion");
            DropColumn("dbo.RegistrosCapacitaciones", "UsuarioModificacion");
        }
    }
}
