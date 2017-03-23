namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Configuracion_Mantenible : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Configuracion", "Label", c => c.String());
            AddColumn("dbo.Configuracion", "Order", c => c.Int(nullable: false));
            AddColumn("dbo.Configuracion", "Seccion", c => c.String());
            AddColumn("dbo.Configuracion", "SubSeccion", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Configuracion", "SubSeccion");
            DropColumn("dbo.Configuracion", "Seccion");
            DropColumn("dbo.Configuracion", "Order");
            DropColumn("dbo.Configuracion", "Label");
        }
    }
}
