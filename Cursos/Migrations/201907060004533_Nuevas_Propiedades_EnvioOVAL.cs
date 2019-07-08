namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Nuevas_Propiedades_EnvioOVAL : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RegistrosCapacitaciones", "EnvioOVALEstado", c => c.Int(nullable: false));
            AddColumn("dbo.RegistrosCapacitaciones", "EnvioOVALFechaHora", c => c.DateTime());
            AddColumn("dbo.RegistrosCapacitaciones", "EnvioOVALUsuario", c => c.String());
            AddColumn("dbo.RegistrosCapacitaciones", "EnvioOVALMensaje", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.RegistrosCapacitaciones", "EnvioOVALMensaje");
            DropColumn("dbo.RegistrosCapacitaciones", "EnvioOVALUsuario");
            DropColumn("dbo.RegistrosCapacitaciones", "EnvioOVALFechaHora");
            DropColumn("dbo.RegistrosCapacitaciones", "EnvioOVALEstado");
        }
    }
}
