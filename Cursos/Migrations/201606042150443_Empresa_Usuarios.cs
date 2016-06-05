namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Empresa_Usuarios : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.EmpresaUsuarios",
                c => new
                    {
                        EmpresaUsuarioId = c.Int(nullable: false, identity: true),
                        Usuario = c.String(),
                        EmpresaID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.EmpresaUsuarioId)
                .ForeignKey("dbo.Empresas", t => t.EmpresaID, cascadeDelete: true)
                .Index(t => t.EmpresaID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.EmpresaUsuarios", "EmpresaID", "dbo.Empresas");
            DropIndex("dbo.EmpresaUsuarios", new[] { "EmpresaID" });
            DropTable("dbo.EmpresaUsuarios");
        }
    }
}
