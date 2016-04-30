namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Capacitado_FechaCumpelanos : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Capacitados", "Fecha", c => c.DateTime());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Capacitados", "Fecha", c => c.DateTime(nullable: false));
        }
    }
}
