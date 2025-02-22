namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class JornadaActaEnviada : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.JornadaActaEnviadas",
                c => new
                    {
                        JornadaActaEnviadaID = c.Int(nullable: false, identity: true),
                        JornadaID = c.Int(nullable: false),
                        UsuarioEnvio = c.String(nullable: false),
                        FechaHoraEnvio = c.DateTime(nullable: false),
                        MailDestinoEnvio = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.JornadaActaEnviadaID)
                .ForeignKey("dbo.Jornadas", t => t.JornadaID, cascadeDelete: true)
                .Index(t => t.JornadaID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.JornadaActaEnviadas", "JornadaID", "dbo.Jornadas");
            DropIndex("dbo.JornadaActaEnviadas", new[] { "JornadaID" });
            DropTable("dbo.JornadaActaEnviadas");
        }
    }
}
