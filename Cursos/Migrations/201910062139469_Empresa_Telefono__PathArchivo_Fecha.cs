namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Empresa_Telefono__PathArchivo_Fecha : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Empresas", "Telefono", c => c.String());
            AddColumn("dbo.PathsArchivos", "FechaArchivo", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PathsArchivos", "FechaArchivo");
            DropColumn("dbo.Empresas", "Telefono");
        }
    }
}
