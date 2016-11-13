namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Foto_Capacitado_Actualizacion : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PathFotoCapacitadoes", "NombreArchivo", c => c.String(maxLength: 255));
            DropColumn("dbo.PathFotoCapacitadoes", "PathArchivo");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PathFotoCapacitadoes", "PathArchivo", c => c.String(maxLength: 255));
            DropColumn("dbo.PathFotoCapacitadoes", "NombreArchivo");
        }
    }
}
