namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CursoDocumentacionAdicionalInscripcion : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cursos", "RequiereDocumentacionAdicionalInscripcion", c => c.Boolean(nullable: false));
            AddColumn("dbo.Cursos", "DocumentacionAdicionalIdentificador", c => c.String());
            AddColumn("dbo.Cursos", "RequiereDocumentacionAdicionalInscripcionObligatoria", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cursos", "RequiereDocumentacionAdicionalInscripcionObligatoria");
            DropColumn("dbo.Cursos", "DocumentacionAdicionalIdentificador");
            DropColumn("dbo.Cursos", "RequiereDocumentacionAdicionalInscripcion");
        }
    }
}
