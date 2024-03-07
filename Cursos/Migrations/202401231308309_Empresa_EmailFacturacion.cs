namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Empresa_EmailFacturacion : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Empresas", "EmailFacturacion", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Empresas", "EmailFacturacion");
        }
    }
}
