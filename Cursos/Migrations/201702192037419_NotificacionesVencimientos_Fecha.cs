namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NotificacionesVencimientos_Fecha : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.NotificacionesVencimientos", "Fecha", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.NotificacionesVencimientos", "Fecha");
        }
    }
}
