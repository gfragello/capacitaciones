namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LimitarDocumentoALongitud25 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Capacitados", "Documento", c => c.String(nullable: false, maxLength: 25));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Capacitados", "Documento", c => c.String(nullable: false));
        }
    }
}
