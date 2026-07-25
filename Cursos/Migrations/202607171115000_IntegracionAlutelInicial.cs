namespace Cursos.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class IntegracionAlutelInicial : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cursos", "TipoVigenciaAlutel", c => c.Int());

            CreateTable(
                "dbo.OperacionesIntegracionAlutel",
                c => new
                    {
                        OperacionIntegracionAlutelID = c.Long(nullable: false, identity: true),
                        RegistroCapacitacionID = c.Int(),
                        DocumentoSnapshot = c.String(nullable: false, maxLength: 64),
                        TipoVigencia = c.Int(nullable: false),
                        FechaVigencia = c.DateTime(nullable: false),
                        Estado = c.Int(nullable: false),
                        FechaCreacionUtc = c.DateTime(nullable: false),
                        FechaActualizacionUtc = c.DateTime(nullable: false),
                        UsuarioOrigen = c.String(nullable: false, maxLength: 256),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.OperacionIntegracionAlutelID)
                .ForeignKey("dbo.RegistrosCapacitaciones", t => t.RegistroCapacitacionID, cascadeDelete: true)
                .Index(t => t.RegistroCapacitacionID, name: "IX_OperacionAlutel_Registro")
                .Index(t => t.DocumentoSnapshot, name: "IX_OperacionAlutel_Documento")
                .Index(t => t.Estado, name: "IX_OperacionAlutel_Estado");

            CreateTable(
                "dbo.IntentosIntegracionAlutel",
                c => new
                    {
                        IntentoIntegracionAlutelID = c.Long(nullable: false, identity: true),
                        OperacionIntegracionAlutelID = c.Long(nullable: false),
                        BatchId = c.Guid(nullable: false),
                        NumeroIntento = c.Int(nullable: false),
                        Entorno = c.String(nullable: false, maxLength: 32),
                        FechaInicioUtc = c.DateTime(nullable: false),
                        FechaFinUtc = c.DateTime(),
                        CodigoHttp = c.Int(),
                        ResultadoTecnico = c.Int(nullable: false),
                        MensajeSanitizado = c.String(maxLength: 1000),
                        ProcesadosCorrectamente = c.Int(),
                        ProcesadosConError = c.Int(),
                        CorrelationIdProveedor = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.IntentoIntegracionAlutelID)
                .ForeignKey("dbo.OperacionesIntegracionAlutel", t => t.OperacionIntegracionAlutelID, cascadeDelete: true)
                .Index(t => new { t.OperacionIntegracionAlutelID, t.NumeroIntento }, unique: true, name: "IX_IntentoAlutel_OperacionNumero")
                .Index(t => t.BatchId, name: "IX_IntentoAlutel_Lote");

            Sql(@"UPDATE dbo.Cursos
                  SET TipoVigenciaAlutel =
                      CASE CursoID
                          WHEN 1 THEN 1
                          WHEN 2 THEN 3
                          WHEN 3 THEN 2
                      END
                  WHERE CursoID IN (1, 2, 3)");
        }

        public override void Down()
        {
            DropForeignKey("dbo.IntentosIntegracionAlutel", "OperacionIntegracionAlutelID", "dbo.OperacionesIntegracionAlutel");
            DropForeignKey("dbo.OperacionesIntegracionAlutel", "RegistroCapacitacionID", "dbo.RegistrosCapacitaciones");
            DropIndex("dbo.IntentosIntegracionAlutel", "IX_IntentoAlutel_Lote");
            DropIndex("dbo.IntentosIntegracionAlutel", "IX_IntentoAlutel_OperacionNumero");
            DropIndex("dbo.OperacionesIntegracionAlutel", "IX_OperacionAlutel_Estado");
            DropIndex("dbo.OperacionesIntegracionAlutel", "IX_OperacionAlutel_Documento");
            DropIndex("dbo.OperacionesIntegracionAlutel", "IX_OperacionAlutel_Registro");
            DropTable("dbo.IntentosIntegracionAlutel");
            DropTable("dbo.OperacionesIntegracionAlutel");
            DropColumn("dbo.Cursos", "TipoVigenciaAlutel");
        }
    }
}