namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Quitar_CascadeDelete_TiposDocumento : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Capacitados", "TipoDocumentoID", "dbo.TiposDocumento");
            AddForeignKey("dbo.Capacitados", "TipoDocumentoID", "dbo.TiposDocumento", "TipoDocumentoID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Capacitados", "TipoDocumentoID", "dbo.TiposDocumento");
            AddForeignKey("dbo.Capacitados", "TipoDocumentoID", "dbo.TiposDocumento", "TipoDocumentoID", cascadeDelete: true);
        }
    }
}
