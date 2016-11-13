namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Eliminar_Registros_Jornada_Cascada : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.RegistrosCapacitaciones", "JornadaID", "dbo.Jornadas");
            AddForeignKey("dbo.RegistrosCapacitaciones", "JornadaID", "dbo.Jornadas", "JornadaID", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RegistrosCapacitaciones", "JornadaID", "dbo.Jornadas");
            AddForeignKey("dbo.RegistrosCapacitaciones", "JornadaID", "dbo.Jornadas", "JornadaID");
        }
    }
}
