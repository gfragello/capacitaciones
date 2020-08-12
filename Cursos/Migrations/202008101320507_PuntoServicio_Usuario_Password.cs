namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PuntoServicio_Usuario_Password : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PuntosServicio", "Usuario", c => c.String());
            AddColumn("dbo.PuntosServicio", "Password", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.PuntosServicio", "Password");
            DropColumn("dbo.PuntosServicio", "Usuario");
        }
    }
}
