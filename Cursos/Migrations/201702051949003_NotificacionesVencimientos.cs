namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NotificacionesVencimientos : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.NotificacionesVencimientos",
                c => new
                    {
                        NotificacionVencimientoID = c.Int(nullable: false, identity: true),
                        MailNotificacionVencimiento = c.String(),
                        Estado = c.Int(nullable: false),
                        RegistroCapacitacionID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.NotificacionVencimientoID)
                .ForeignKey("dbo.RegistrosCapacitaciones", t => t.RegistroCapacitacionID, cascadeDelete: true)
                .Index(t => t.RegistroCapacitacionID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.NotificacionesVencimientos", "RegistroCapacitacionID", "dbo.RegistrosCapacitaciones");
            DropIndex("dbo.NotificacionesVencimientos", new[] { "RegistroCapacitacionID" });
            DropTable("dbo.NotificacionesVencimientos");
        }
    }
}
