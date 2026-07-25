using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cursos.Models;

namespace Cursos.Integraciones.Alutel.Dominio
{
    [Table("OperacionesIntegracionAlutel")]
    public class OperacionIntegracionAlutel
    {
        public OperacionIntegracionAlutel()
        {
            Intentos = new HashSet<IntentoIntegracionAlutel>();
        }

        public long OperacionIntegracionAlutelID { get; set; }

        [Index("IX_OperacionAlutel_Registro")]
        public int? RegistroCapacitacionID { get; set; }

        public virtual RegistroCapacitacion RegistroCapacitacion { get; set; }

        [Required]
        [StringLength(64)]
        [Index("IX_OperacionAlutel_Documento")]
        public string DocumentoSnapshot { get; set; }

        public TipoVigenciaAlutel TipoVigencia { get; set; }

        public DateTime FechaVigencia { get; set; }

        [Index("IX_OperacionAlutel_Estado")]
        public EstadoIntegracionAlutel Estado { get; set; }

        public DateTime FechaCreacionUtc { get; set; }

        public DateTime FechaActualizacionUtc { get; set; }

        [Required]
        [StringLength(256)]
        public string UsuarioOrigen { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public virtual ICollection<IntentoIntegracionAlutel> Intentos { get; set; }
    }
}
