using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using Cursos.Models.Enums;

namespace Cursos.Models
{
    [Table("RegistrosCapacitaciones")]
    public class RegistroCapacitacion : ElementoAccesoControlado
    {
        public int RegistroCapacitacionID { get; set; }

        [Required(AllowEmptyStrings = false)]
        public bool Aprobado { get; set; }

        [NotMapped]
        public string AprobadoTexto
        {
            get
            {
                if (this.Aprobado)
                    return "Aprobado";
                else
                    return "No Aprobado";
            }    
        }

        [Required(ErrorMessage = "Debe ingresar la nota obtenida")]
        public int Nota { get; set; }

        [Display(Name = "Nota previa")]
        public int? NotaPrevia { get; set; }

        [Required(ErrorMessage = "Debe seleccionar la Jornada")]
        [Display(Name = "Jornada")]
        public int JornadaID { get; set; }
        public virtual Jornada Jornada { get; set; }

        public int CapacitadoID { get; set; }
        public virtual Capacitado Capacitado { get; set; }

        [Display(Name = "Fecha Vencimiento")]
        [Required(ErrorMessage = "Debe ingresar la {0}")]
        [DisplayFormat(DataFormatString = "{0:d}", ApplyFormatInEditMode = true)]
        public DateTime FechaVencimiento { get; set; }

        public EstadosRegistroCapacitacion Estado { get; set; }

        [NotMapped]
        private DateTime FechaInicioNoficacionVencimiento
        {
            get
            {
                const double diasAnticipacionAviso = 30;
                return this.FechaVencimiento.AddDays(diasAnticipacionAviso);
            }
        }

        [NotMapped]
        public bool NotificarRegistroPorVencer
        {
            get
            {
                //DateTime fechaInicioControlVencimientos = new DateTime(2016, 1, 15);
                return this.FechaInicioNoficacionVencimiento >= DateTime.Now;
            }
        }

        [NotMapped]
        public TipoEliminacionEnum TipoEliminacionPermitida
        {
            get
            {
                bool puedeEliminar = false;

                if (HttpContext.Current.User.IsInRole("Administrador"))
                {
                    puedeEliminar = true;
                }
                else if (HttpContext.Current.User.IsInRole("IncripcionesExternas"))
                {
                    if (this.UsuarioModificacion == HttpContext.Current.User.Identity.Name && this.Estado == EstadosRegistroCapacitacion.Inscripto)
                        puedeEliminar = true;
                }

                if (puedeEliminar)
                {
                    if (this.Estado == EstadosRegistroCapacitacion.Inscripto)
                    {
                        return TipoEliminacionEnum.EliminacionSimple;
                    }
                    else
                    {
                        return TipoEliminacionEnum.EliminacionConRevision;
                    }
                }

                return TipoEliminacionEnum.NoEliminar;
            }
        }


        [NotMapped]
        public bool FueCalificado
        {
            get
            {
                return this.Estado == EstadosRegistroCapacitacion.Aprobado || this.Estado == EstadosRegistroCapacitacion.NoAprobado;
            }
        }


        public bool Calificar(int nota)
        {
            EstadosRegistroCapacitacion estadoFinal = EstadosRegistroCapacitacion.Aprobado;

            if (this.Jornada.Curso.EvaluacionConNota)
            {
                if (this.Jornada.Curso.PuntajeMaximo > 0 && nota > this.Jornada.Curso.PuntajeMaximo)
                    return false;

                if (this.Jornada.Curso.PuntajeMinimo > 0 && nota < this.Jornada.Curso.PuntajeMinimo)
                    estadoFinal = EstadosRegistroCapacitacion.NoAprobado;

                this.Nota = nota;
                this.Estado = estadoFinal;

                return true;
            }
            else
                return false;
        }

        public bool Calificar(bool aprobado)
        {
            if (!this.Jornada.Curso.EvaluacionConNota)
            {
                if (aprobado)
                    this.Estado = EstadosRegistroCapacitacion.Aprobado;
                else
                    this.Estado = EstadosRegistroCapacitacion.NoAprobado;

                return true;
            }
            else
                return false;
        }

        public void BorrarCalificacion()
        {
            this.Nota = 0;
            this.NotaPrevia = 0;
            this.Estado = EstadosRegistroCapacitacion.Inscripto;
        }
    }
}