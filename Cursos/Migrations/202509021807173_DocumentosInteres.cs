namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DocumentosInteres : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DocumentosInteres",
                c => new
                    {
                        DocumentoInteresID = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 200),
                        Descripcion = c.String(maxLength: 1000),
                        NombreArchivo = c.String(nullable: false, maxLength: 255),
                        Activo = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.DocumentoInteresID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.DocumentosInteres");
        }
    }
}
