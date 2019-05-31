namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Nuevas_Propiedades_Jornadas : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Jornadas", "TieneMinimoAsistentes", c => c.Boolean(nullable: false));
            AddColumn("dbo.Jornadas", "MinimoAsistentes", c => c.Int(nullable: false));
            AddColumn("dbo.Jornadas", "TieneMaximoAsistentes", c => c.Boolean(nullable: false));
            AddColumn("dbo.Jornadas", "MaximoAsistentes", c => c.Int(nullable: false));
            AddColumn("dbo.Jornadas", "TieneCierreIncripcion", c => c.Boolean(nullable: false));
            AddColumn("dbo.Jornadas", "HorasCierreInscripcion", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Jornadas", "HorasCierreInscripcion");
            DropColumn("dbo.Jornadas", "TieneCierreIncripcion");
            DropColumn("dbo.Jornadas", "MaximoAsistentes");
            DropColumn("dbo.Jornadas", "TieneMaximoAsistentes");
            DropColumn("dbo.Jornadas", "MinimoAsistentes");
            DropColumn("dbo.Jornadas", "TieneMinimoAsistentes");
        }
    }
}
