namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Correciones : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Capacitados", "TipoDocumentoID", "dbo.TiposDocumento");
            DropIndex("dbo.Capacitados", new[] { "TipoDocumentoID" });
            AddColumn("dbo.TiposDocumento", "Abreviacion", c => c.String(maxLength: 4));
            AlterColumn("dbo.Capacitados", "TipoDocumentoID", c => c.Int(nullable: false));
            CreateIndex("dbo.Capacitados", "TipoDocumentoID");
            AddForeignKey("dbo.Capacitados", "TipoDocumentoID", "dbo.TiposDocumento", "TipoDocumentoID", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Capacitados", "TipoDocumentoID", "dbo.TiposDocumento");
            DropIndex("dbo.Capacitados", new[] { "TipoDocumentoID" });
            AlterColumn("dbo.Capacitados", "TipoDocumentoID", c => c.Int());
            DropColumn("dbo.TiposDocumento", "Abreviacion");
            CreateIndex("dbo.Capacitados", "TipoDocumentoID");
            AddForeignKey("dbo.Capacitados", "TipoDocumentoID", "dbo.TiposDocumento", "TipoDocumentoID");
        }
    }
}
