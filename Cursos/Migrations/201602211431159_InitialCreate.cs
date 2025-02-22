namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            // === Tablas de Identidad (ApplicationDbContext) ===
            CreateTable(
                "dbo.AspNetRoles",
                c => new
                {
                    Id = c.String(nullable: false, maxLength: 128),
                    Name = c.String(nullable: false, maxLength: 256),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");

            CreateTable(
                "dbo.AspNetUserRoles",
                c => new
                {
                    UserId = c.String(nullable: false, maxLength: 128),
                    RoleId = c.String(nullable: false, maxLength: 128),
                })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.AspNetRoles", t => t.RoleId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);

            CreateTable(
                "dbo.AspNetUsers",
                c => new
                {
                    Id = c.String(nullable: false, maxLength: 128),
                    Email = c.String(maxLength: 256),
                    EmailConfirmed = c.Boolean(nullable: false),
                    PasswordHash = c.String(),
                    SecurityStamp = c.String(),
                    PhoneNumber = c.String(),
                    PhoneNumberConfirmed = c.Boolean(nullable: false),
                    TwoFactorEnabled = c.Boolean(nullable: false),
                    LockoutEndDateUtc = c.DateTime(),
                    LockoutEnabled = c.Boolean(nullable: false),
                    AccessFailedCount = c.Int(nullable: false),
                    UserName = c.String(nullable: false, maxLength: 256),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");

            CreateTable(
                "dbo.AspNetUserClaims",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    UserId = c.String(nullable: false, maxLength: 128),
                    ClaimType = c.String(),
                    ClaimValue = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);

            CreateTable(
                "dbo.AspNetUserLogins",
                c => new
                {
                    LoginProvider = c.String(nullable: false, maxLength: 128),
                    ProviderKey = c.String(nullable: false, maxLength: 128),
                    UserId = c.String(nullable: false, maxLength: 128),
                })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);

            // === Tablas del Dominio (antiguamente pertenecía al contexto CursosDbContext (eliminado en 202502 cuando se unificaron ambos contextos)) ===
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
            // Primero, eliminar las tablas del Dominio
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

            // Luego, eliminar las tablas de Identidad
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            DropIndex("dbo.AspNetUserLogins", new[] { "UserId" });
            DropIndex("dbo.AspNetUserClaims", new[] { "UserId" });
            DropIndex("dbo.AspNetUsers", "UserNameIndex");
            DropIndex("dbo.AspNetUserRoles", new[] { "RoleId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "UserId" });
            DropIndex("dbo.AspNetRoles", "RoleNameIndex");
            DropTable("dbo.AspNetUserLogins");
            DropTable("dbo.AspNetUserClaims");
            DropTable("dbo.AspNetUsers");
            DropTable("dbo.AspNetUserRoles");
            DropTable("dbo.AspNetRoles");
        }
    }
}
