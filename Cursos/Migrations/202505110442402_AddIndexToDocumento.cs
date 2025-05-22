namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIndexToDocumento : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.Capacitados", "Documento", name: "IX_Capacitado_Documento");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Capacitados", "IX_Capacitado_Documento");
        }
    }
}
