namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PuntoServicio_Rest : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PuntosServicio", "DireccionRequest", c => c.String());
            AddColumn("dbo.PuntosServicio", "DireccionToken", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.PuntosServicio", "DireccionToken");
            DropColumn("dbo.PuntosServicio", "DireccionRequest");
        }
    }
}
