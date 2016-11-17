namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Acta_Jornada : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.PathFotoCapacitadoes", "PathFotoCapacitadoId", "dbo.Capacitados");
            DropIndex("dbo.PathFotoCapacitadoes", new[] { "PathFotoCapacitadoId" });
            CreateTable(
                "dbo.PathsArchivos",
                c => new
                    {
                        PathArchivoId = c.Int(nullable: false, identity: true),
                        NombreArchivo = c.String(maxLength: 255),
                        TipoArchivo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.PathArchivoId);
            
            AddColumn("dbo.Capacitados", "PathArchivoID", c => c.Int());
            AddColumn("dbo.Jornadas", "PathArchivoID", c => c.Int());
            CreateIndex("dbo.Capacitados", "PathArchivoID");
            CreateIndex("dbo.Jornadas", "PathArchivoID");
            AddForeignKey("dbo.Capacitados", "PathArchivoID", "dbo.PathsArchivos", "PathArchivoId");
            AddForeignKey("dbo.Jornadas", "PathArchivoID", "dbo.PathsArchivos", "PathArchivoId");
            DropColumn("dbo.Capacitados", "PathFotoCapacitadoID");
            DropTable("dbo.PathFotoCapacitadoes");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.PathFotoCapacitadoes",
                c => new
                    {
                        PathFotoCapacitadoId = c.Int(nullable: false),
                        NombreArchivo = c.String(maxLength: 255),
                        TipoArchivo = c.Int(nullable: false),
                        CapacitadoID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.PathFotoCapacitadoId);
            
            AddColumn("dbo.Capacitados", "PathFotoCapacitadoID", c => c.Int(nullable: false));
            DropForeignKey("dbo.Jornadas", "PathArchivoID", "dbo.PathsArchivos");
            DropForeignKey("dbo.Capacitados", "PathArchivoID", "dbo.PathsArchivos");
            DropIndex("dbo.Jornadas", new[] { "PathArchivoID" });
            DropIndex("dbo.Capacitados", new[] { "PathArchivoID" });
            DropColumn("dbo.Jornadas", "PathArchivoID");
            DropColumn("dbo.Capacitados", "PathArchivoID");
            DropTable("dbo.PathsArchivos");
            CreateIndex("dbo.PathFotoCapacitadoes", "PathFotoCapacitadoId");
            AddForeignKey("dbo.PathFotoCapacitadoes", "PathFotoCapacitadoId", "dbo.Capacitados", "CapacitadoID");
        }
    }
}
