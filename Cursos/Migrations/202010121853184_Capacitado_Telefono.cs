namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Capacitado_Telefono : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Capacitados", "Telefono", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Capacitados", "Telefono");
        }
    }
}
