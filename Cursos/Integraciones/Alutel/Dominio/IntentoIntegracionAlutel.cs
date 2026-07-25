using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cursos.Integraciones.Alutel.Dominio
{
    [Table("IntentosIntegracionAlutel")]
    public class IntentoIntegracionAlutel
    {
        public long IntentoIntegracionAlutelID { get; set; }

        [Index("IX_IntentoAlutel_OperacionNumero", 1, IsUnique = true)]
        public long OperacionIntegracionAlutelID { get; set; }

        public virtual OperacionIntegracionAlutel Operacion { get; set; }

        [Index("IX_IntentoAlutel_Lote")]
        public Guid BatchId { get; set; }

        [Index("IX_IntentoAlutel_OperacionNumero", 2, IsUnique = true)]
        public int NumeroIntento { get; set; }

        [Required]
        [StringLength(32)]
        public string Entorno { get; set; }

        public DateTime FechaInicioUtc { get; set; }

        public DateTime? FechaFinUtc { get; set; }

        public int? CodigoHttp { get; set; }

        public ResultadoTecnicoAlutel ResultadoTecnico { get; set; }

        [StringLength(1000)]
        public string MensajeSanitizado { get; set; }

        public int? ProcesadosCorrectamente { get; set; }

        public int? ProcesadosConError { get; set; }

        [StringLength(128)]
        public string CorrelationIdProveedor { get; set; }
    }
}
