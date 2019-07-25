namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Tipo_Documento_OVAL : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TiposDocumento", "PermiteEnviosOVAL", c => c.Boolean(nullable: false));
            AddColumn("dbo.TiposDocumento", "TipoDocumentoOVAL", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.TiposDocumento", "TipoDocumentoOVAL");
            DropColumn("dbo.TiposDocumento", "PermiteEnviosOVAL");
        }
    }
}
