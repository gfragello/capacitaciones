namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Estados_RegistrosCapasitacion : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RegistrosCapacitaciones", "Estado", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.RegistrosCapacitaciones", "Estado");
        }
    }
}
