namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Empresa_Departamentos : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Departamentos",
                c => new
                    {
                        DepartamentoID = c.Int(nullable: false, identity: true),
                        Nombre = c.String(maxLength: 25),
                    })
                .PrimaryKey(t => t.DepartamentoID);
            
            AddColumn("dbo.Empresas", "Localidad", c => c.String());
            AddColumn("dbo.Empresas", "CodigoPostal", c => c.String());
            AddColumn("dbo.Empresas", "Email", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Empresas", "Email");
            DropColumn("dbo.Empresas", "CodigoPostal");
            DropColumn("dbo.Empresas", "Localidad");
            DropTable("dbo.Departamentos");
        }
    }
}
