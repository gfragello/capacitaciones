namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InstructorUsuario : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.InstructoresUsuarios",
                c => new
                    {
                        InstructorUsuarioId = c.Int(nullable: false, identity: true),
                        Usuario = c.String(),
                        InstructorID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.InstructorUsuarioId)
                .ForeignKey("dbo.Instructores", t => t.InstructorID, cascadeDelete: true)
                .Index(t => t.InstructorID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.InstructoresUsuarios", "InstructorID", "dbo.Instructores");
            DropIndex("dbo.InstructoresUsuarios", new[] { "InstructorID" });
            DropTable("dbo.InstructoresUsuarios");
        }
    }
}
