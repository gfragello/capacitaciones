namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Elemento_Acceso_Controlado_Empresa : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Empresas", "UsuarioModificacion", c => c.String());
            AddColumn("dbo.Empresas", "FechaModficacion", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Empresas", "FechaModficacion");
            DropColumn("dbo.Empresas", "UsuarioModificacion");
        }
    }
}
