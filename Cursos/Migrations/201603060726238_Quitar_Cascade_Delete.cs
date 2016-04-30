namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Quitar_Cascade_Delete : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Jornadas", "LugarID", "dbo.Lugares");
            AddForeignKey("dbo.Jornadas", "LugarID", "dbo.Lugares", "LugarID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Jornadas", "LugarID", "dbo.Lugares");
            AddForeignKey("dbo.Jornadas", "LugarID", "dbo.Lugares", "LugarID", cascadeDelete: true);
        }
    }
}
