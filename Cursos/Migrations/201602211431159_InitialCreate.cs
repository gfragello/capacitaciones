namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Capacitadoes",
                c => new
                    {
                        CapacitadoID = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false),
                        Apellido = c.String(nullable: false),
                        Documento = c.String(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        EmpresaID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.CapacitadoID)
                .ForeignKey("dbo.Empresas", t => t.EmpresaID, cascadeDelete: true)
                .Index(t => t.EmpresaID);
            
            CreateTable(
                "dbo.Empresas",
                c => new
                    {
                        EmpresaID = c.Int(nullable: false, identity: true),
                        NombreFantasia = c.String(nullable: false),
                        Domicilio = c.String(),
                        RazonSocial = c.String(nullable: false),
                        RUT = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.EmpresaID);
            
            CreateTable(
                "dbo.Cursoes",
                c => new
                    {
                        CursoID = c.Int(nullable: false, identity: true),
                        Descripcion = c.String(nullable: false),
                        Costo = c.Int(nullable: false),
                        Horas = c.Int(nullable: false),
                        Modulo = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.CursoID);
            
            CreateTable(
                "dbo.Jornadas",
                c => new
                    {
                        JornadaID = c.Int(nullable: false, identity: true),
                        Fecha = c.DateTime(nullable: false),
                        CursoId = c.Int(nullable: false),
                        LugarID = c.Int(nullable: false),
                        InstructorId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.JornadaID)
                .ForeignKey("dbo.Cursoes", t => t.CursoId, cascadeDelete: true)
                .ForeignKey("dbo.Instructores", t => t.InstructorId, cascadeDelete: true)
                .ForeignKey("dbo.Lugares", t => t.LugarID, cascadeDelete: true)
                .Index(t => t.CursoId)
                .Index(t => t.LugarID)
                .Index(t => t.InstructorId);
            
            CreateTable(
                "dbo.Instructores",
                c => new
                    {
                        InstructorID = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false),
                        Apellido = c.String(nullable: false),
                        Documento = c.String(),
                        FechaNacimiento = c.DateTime(nullable: false),
                        Domicilio = c.String(),
                        Telefono = c.String(),
                        Activo = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.InstructorID);
            
            CreateTable(
                "dbo.Lugares",
                c => new
                    {
                        LugarID = c.Int(nullable: false, identity: true),
                        NombreLugar = c.String(nullable: false),
                        AbrevLugar = c.String(maxLength: 3),
                    })
                .PrimaryKey(t => t.LugarID);
            
            CreateTable(
                "dbo.RegistrosCapacitaciones",
                c => new
                    {
                        RegistroCapacitacionID = c.Int(nullable: false, identity: true),
                        Aprobado = c.Boolean(nullable: false),
                        Nota = c.Int(nullable: false),
                        JornadaID = c.Int(nullable: false),
                        CapacitadoID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.RegistroCapacitacionID)
                .ForeignKey("dbo.Capacitadoes", t => t.CapacitadoID, cascadeDelete: true)
                .ForeignKey("dbo.Jornadas", t => t.JornadaID, cascadeDelete: true)
                .Index(t => t.JornadaID)
                .Index(t => t.CapacitadoID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RegistrosCapacitaciones", "JornadaID", "dbo.Jornadas");
            DropForeignKey("dbo.RegistrosCapacitaciones", "CapacitadoID", "dbo.Capacitadoes");
            DropForeignKey("dbo.Jornadas", "LugarID", "dbo.Lugares");
            DropForeignKey("dbo.Jornadas", "InstructorId", "dbo.Instructores");
            DropForeignKey("dbo.Jornadas", "CursoId", "dbo.Cursoes");
            DropForeignKey("dbo.Capacitadoes", "EmpresaID", "dbo.Empresas");
            DropIndex("dbo.RegistrosCapacitaciones", new[] { "CapacitadoID" });
            DropIndex("dbo.RegistrosCapacitaciones", new[] { "JornadaID" });
            DropIndex("dbo.Jornadas", new[] { "InstructorId" });
            DropIndex("dbo.Jornadas", new[] { "LugarID" });
            DropIndex("dbo.Jornadas", new[] { "CursoId" });
            DropIndex("dbo.Capacitadoes", new[] { "EmpresaID" });
            DropTable("dbo.RegistrosCapacitaciones");
            DropTable("dbo.Lugares");
            DropTable("dbo.Instructores");
            DropTable("dbo.Jornadas");
            DropTable("dbo.Cursoes");
            DropTable("dbo.Empresas");
            DropTable("dbo.Capacitadoes");
        }
    }
}
