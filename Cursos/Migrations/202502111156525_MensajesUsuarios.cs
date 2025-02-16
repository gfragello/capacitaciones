namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MensajesUsuarios : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MensajesUsuarios",
                c => new
                    {
                        MensajesUsuariosID = c.Int(nullable: false, identity: true),
                        IdentificadorInterno = c.String(),
                        Cuerpo = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.MensajesUsuariosID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.MensajesUsuarios");
        }
    }
}
