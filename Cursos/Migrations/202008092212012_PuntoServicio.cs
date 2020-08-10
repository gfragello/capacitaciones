namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PuntoServicio : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PuntosServicio",
                c => new
                    {
                        PuntoServicioId = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false),
                        Direccion = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.PuntoServicioId);
            
            AddColumn("dbo.Cursos", "PuntoServicioId", c => c.Int());
            CreateIndex("dbo.Cursos", "PuntoServicioId");
            AddForeignKey("dbo.Cursos", "PuntoServicioId", "dbo.PuntosServicio", "PuntoServicioId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Cursos", "PuntoServicioId", "dbo.PuntosServicio");
            DropIndex("dbo.Cursos", new[] { "PuntoServicioId" });
            DropColumn("dbo.Cursos", "PuntoServicioId");
            DropTable("dbo.PuntosServicio");
        }
    }
}
