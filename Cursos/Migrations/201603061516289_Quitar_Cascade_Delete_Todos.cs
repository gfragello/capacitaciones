namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Quitar_Cascade_Delete_Todos : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Capacitados", "EmpresaID", "dbo.Empresas");
            DropForeignKey("dbo.Jornadas", "CursoId", "dbo.Cursos");
            DropForeignKey("dbo.Jornadas", "InstructorId", "dbo.Instructores");
            AddForeignKey("dbo.Capacitados", "EmpresaID", "dbo.Empresas", "EmpresaID");
            AddForeignKey("dbo.Jornadas", "CursoId", "dbo.Cursos", "CursoID");
            AddForeignKey("dbo.Jornadas", "InstructorId", "dbo.Instructores", "InstructorID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Jornadas", "InstructorId", "dbo.Instructores");
            DropForeignKey("dbo.Jornadas", "CursoId", "dbo.Cursos");
            DropForeignKey("dbo.Capacitados", "EmpresaID", "dbo.Empresas");
            AddForeignKey("dbo.Jornadas", "InstructorId", "dbo.Instructores", "InstructorID", cascadeDelete: true);
            AddForeignKey("dbo.Jornadas", "CursoId", "dbo.Cursos", "CursoID", cascadeDelete: true);
            AddForeignKey("dbo.Capacitados", "EmpresaID", "dbo.Empresas", "EmpresaID", cascadeDelete: true);
        }
    }
}
