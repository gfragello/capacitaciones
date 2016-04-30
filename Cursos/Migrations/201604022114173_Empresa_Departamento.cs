namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Empresa_Departamento : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Empresas", "DepartamentoID", c => c.Int(nullable: false, defaultValue: 1));
            CreateIndex("dbo.Empresas", "DepartamentoID");
            AddForeignKey("dbo.Empresas", "DepartamentoID", "dbo.Departamentos", "DepartamentoID", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Empresas", "DepartamentoID", "dbo.Departamentos");
            DropIndex("dbo.Empresas", new[] { "DepartamentoID" });
            DropColumn("dbo.Empresas", "DepartamentoID");
        }
    }
}
