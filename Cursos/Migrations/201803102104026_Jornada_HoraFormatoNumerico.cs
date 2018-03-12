namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Jornada_HoraFormatoNumerico : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Jornadas", "HoraFormatoNumerico", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Jornadas", "HoraFormatoNumerico");
        }
    }
}
