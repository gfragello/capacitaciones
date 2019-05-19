namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Jornadas_Autorizables_Ajuste_Fechas : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Jornadas", "FechaCreacion", c => c.DateTime());
            AlterColumn("dbo.Jornadas", "FechaAutorizacion", c => c.DateTime());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Jornadas", "FechaAutorizacion", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Jornadas", "FechaCreacion", c => c.DateTime(nullable: false));
        }
    }
}
