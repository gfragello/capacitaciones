namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Subdirectorio : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PathsArchivos", "SubDirectorio", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.PathsArchivos", "SubDirectorio");
        }
    }
}
