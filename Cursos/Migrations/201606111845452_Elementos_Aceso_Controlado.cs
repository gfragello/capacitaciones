namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Elementos_Aceso_Controlado : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Capacitados", "UsuarioModificacion", c => c.String());
            AddColumn("dbo.Capacitados", "FechaModficacion", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Capacitados", "FechaModficacion");
            DropColumn("dbo.Capacitados", "UsuarioModificacion");
        }
    }
}
