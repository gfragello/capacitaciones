namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Foto_Capacitado : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PathFotoCapacitadoes",
                c => new
                    {
                        PathFotoCapacitadoId = c.Int(nullable: false),
                        PathArchivo = c.String(maxLength: 255),
                        TipoArchivo = c.Int(nullable: false),
                        CapacitadoID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.PathFotoCapacitadoId)
                .ForeignKey("dbo.Capacitados", t => t.PathFotoCapacitadoId)
                .Index(t => t.PathFotoCapacitadoId);
            
            AddColumn("dbo.Capacitados", "PathFotoCapacitadoID", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PathFotoCapacitadoes", "PathFotoCapacitadoId", "dbo.Capacitados");
            DropIndex("dbo.PathFotoCapacitadoes", new[] { "PathFotoCapacitadoId" });
            DropColumn("dbo.Capacitados", "PathFotoCapacitadoID");
            DropTable("dbo.PathFotoCapacitadoes");
        }
    }
}
