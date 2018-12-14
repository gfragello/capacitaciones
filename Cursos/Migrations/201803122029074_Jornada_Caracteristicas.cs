namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Jornada_Caracteristicas : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Jornadas", "Caracteristicas", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Jornadas", "Caracteristicas");
        }
    }
}
