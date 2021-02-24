namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DocumentacionAdicionalDatos : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RegistrosCapacitaciones", "DocumentacionAdicionalDatos", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.RegistrosCapacitaciones", "DocumentacionAdicionalDatos");
        }
    }
}
