namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Nueva_Propiedad_Jornada_Envio_OVAL : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Jornadas", "PermiteEnviosOVAL", c => c.Boolean(nullable: false));
            AddColumn("dbo.Cursos", "PermiteEnviosOVAL", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cursos", "PermiteEnviosOVAL");
            DropColumn("dbo.Jornadas", "PermiteEnviosOVAL");
        }
    }
}
