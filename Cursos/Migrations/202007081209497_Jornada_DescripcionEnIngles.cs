namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Jornada_DescripcionEnIngles : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cursos", "DescripcionEnIngles", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cursos", "DescripcionEnIngles");
        }
    }
}
