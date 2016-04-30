namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Registros_Jornadas : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.RegistrosCapacitaciones", "JornadaID", "dbo.Jornadas");
            AddForeignKey("dbo.RegistrosCapacitaciones", "JornadaID", "dbo.Jornadas", "JornadaID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RegistrosCapacitaciones", "JornadaID", "dbo.Jornadas");
            AddForeignKey("dbo.RegistrosCapacitaciones", "JornadaID", "dbo.Jornadas", "JornadaID", cascadeDelete: true);
        }
    }
}
