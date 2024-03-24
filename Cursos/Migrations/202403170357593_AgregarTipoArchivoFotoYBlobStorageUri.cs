namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AgregarTipoArchivoFotoYBlobStorageUri : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Capacitados", "TipoArchivoFoto", c => c.Int());
            AddColumn("dbo.Capacitados", "BlobStorageUri", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Capacitados", "BlobStorageUri");
            DropColumn("dbo.Capacitados", "TipoArchivoFoto");
        }
    }
}
