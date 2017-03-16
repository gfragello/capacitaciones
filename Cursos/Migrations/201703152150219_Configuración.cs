namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Configuración : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Configuracion",
                c => new
                    {
                        ConfiguracionID = c.Int(nullable: false, identity: true),
                        Index = c.String(),
                        Value = c.String(),
                    })
                .PrimaryKey(t => t.ConfiguracionID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Configuracion");
        }
    }
}
