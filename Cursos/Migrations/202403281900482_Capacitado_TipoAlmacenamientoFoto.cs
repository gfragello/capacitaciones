namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Capacitado_TipoAlmacenamientoFoto : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Capacitados", "TipoAlmacenamientoFoto", c => c.Int());
            DropColumn("dbo.Capacitados", "TipoArchivoFoto");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Capacitados", "TipoArchivoFoto", c => c.Int());
            DropColumn("dbo.Capacitados", "TipoAlmacenamientoFoto");
        }
    }
}
