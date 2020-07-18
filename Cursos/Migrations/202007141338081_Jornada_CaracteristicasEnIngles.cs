namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Jornada_CaracteristicasEnIngles : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Jornadas", "CaracteristicasEnIngles", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Jornadas", "CaracteristicasEnIngles");
        }
    }
}
