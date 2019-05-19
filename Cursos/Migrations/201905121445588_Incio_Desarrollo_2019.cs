namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Incio_Desarrollo_2019 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Cursos", "ColorDeFondo", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Cursos", "ColorDeFondo", c => c.String());
        }
    }
}
