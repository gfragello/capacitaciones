namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Nuevas_Propiedades_Cursos : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cursos", "TieneMinimoAsistentes", c => c.Boolean(nullable: false));
            AddColumn("dbo.Cursos", "MinimoAsistentes", c => c.Int(nullable: false));
            AddColumn("dbo.Cursos", "TieneMaximoAsistentes", c => c.Boolean(nullable: false));
            AddColumn("dbo.Cursos", "MaximoAsistentes", c => c.Int(nullable: false));
            AddColumn("dbo.Cursos", "TieneCierreIncripcion", c => c.Boolean(nullable: false));
            AddColumn("dbo.Cursos", "HorasCierreInscripcion", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cursos", "HorasCierreInscripcion");
            DropColumn("dbo.Cursos", "TieneCierreIncripcion");
            DropColumn("dbo.Cursos", "MaximoAsistentes");
            DropColumn("dbo.Cursos", "TieneMaximoAsistentes");
            DropColumn("dbo.Cursos", "MinimoAsistentes");
            DropColumn("dbo.Cursos", "TieneMinimoAsistentes");
        }
    }
}
