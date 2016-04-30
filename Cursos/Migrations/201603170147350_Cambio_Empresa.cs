namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Cambio_Empresa : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Empresas", "RazonSocial", c => c.String());
            AlterColumn("dbo.Empresas", "RUT", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Empresas", "RUT", c => c.String(nullable: false));
            AlterColumn("dbo.Empresas", "RazonSocial", c => c.String(nullable: false));
        }
    }
}
