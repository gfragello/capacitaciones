namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NotaPrevia_Opcional : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.RegistrosCapacitaciones", "NotaPrevia", c => c.Int());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.RegistrosCapacitaciones", "NotaPrevia", c => c.Int(nullable: false));
        }
    }
}
