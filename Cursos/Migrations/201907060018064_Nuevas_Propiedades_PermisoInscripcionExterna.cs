namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Nuevas_Propiedades_PermisoInscripcionExterna : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Jornadas", "PermiteInscripcionesExternas", c => c.Boolean(nullable: false));
            AddColumn("dbo.Cursos", "PermiteInscripcionesExternas", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cursos", "PermiteInscripcionesExternas");
            DropColumn("dbo.Jornadas", "PermiteInscripcionesExternas");
        }
    }
}
