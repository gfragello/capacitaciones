namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Nuevos_Atributos : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.Capacitadoes", newName: "Capacitados");
            RenameTable(name: "dbo.Cursoes", newName: "Cursos");
            CreateTable(
                "dbo.TiposDocumento",
                c => new
                    {
                        TipoDocumentoID = c.Int(nullable: false, identity: true),
                        Descripcion = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.TipoDocumentoID);
            
            AddColumn("dbo.Capacitados", "TipoDocumentoID", c => c.Int());
            AddColumn("dbo.RegistrosCapacitaciones", "NotaPrevia", c => c.Int(nullable: false));
            CreateIndex("dbo.Capacitados", "TipoDocumentoID");
            AddForeignKey("dbo.Capacitados", "TipoDocumentoID", "dbo.TiposDocumento", "TipoDocumentoID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Capacitados", "TipoDocumentoID", "dbo.TiposDocumento");
            DropIndex("dbo.Capacitados", new[] { "TipoDocumentoID" });
            DropColumn("dbo.RegistrosCapacitaciones", "NotaPrevia");
            DropColumn("dbo.Capacitados", "TipoDocumentoID");
            DropTable("dbo.TiposDocumento");
            RenameTable(name: "dbo.Cursos", newName: "Cursoes");
            RenameTable(name: "dbo.Capacitados", newName: "Capacitadoes");
        }
    }
}
