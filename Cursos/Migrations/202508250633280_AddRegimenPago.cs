namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRegimenPago : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.RegimenesPago",
                c => new
                    {
                        RegimenPagoID = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false),
                        Descripcion = c.String(),
                    })
                .PrimaryKey(t => t.RegimenPagoID);
            
            AddColumn("dbo.Empresas", "RegimenPagoID", c => c.Int());
            CreateIndex("dbo.Empresas", "RegimenPagoID");
            AddForeignKey("dbo.Empresas", "RegimenPagoID", "dbo.RegimenesPago", "RegimenPagoID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Empresas", "RegimenPagoID", "dbo.RegimenesPago");
            DropIndex("dbo.Empresas", new[] { "RegimenPagoID" });
            DropColumn("dbo.Empresas", "RegimenPagoID");
            DropTable("dbo.RegimenesPago");
        }
    }
}
