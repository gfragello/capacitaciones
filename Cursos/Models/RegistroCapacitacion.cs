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

        public virtual ICollection<NotificacionVencimiento> NotificacionesVencimiento { get; set; }

        [Display(Name = "Fecha Vencimiento")]
        [DisplayFormat(DataFormatString = "{0:d}", ApplyFormatInEditMode = true, NullDisplayText = "Sin vencimiento")]
        public DateTime? FechaVencimiento { get; set; }

        public EstadosRegistroCapacitacion Estado { get; set; }

        [Required]
        [Display(Name = "Estado OVAL")]
        public EstadosEnvioOVAL EnvioOVALEstado { get; set; }

        [Display(Name = "Fecha último envio")]
        public DateTime? EnvioOVALFechaHora { get; set; }

        [Display(Name = "Usuario último envio")]
        public string EnvioOVALUsuario { get; set; }

        [Display(Name = "Mensaje último envio")]
        public string EnvioOVALMensaje { get; set; }

        [Display(Name = "Datos adicionales")]
        public string DocumentacionAdicionalDatos { get; set; }

        [NotMapped]
        private DateTime? FechaInicioNoficacionVencimiento
        {
            get
            {
                const double diasAnticipacionAviso = 30;
                if (!this.FechaVencimiento.HasValue)
                    return null;

                return this.FechaVencimiento.Value.AddDays(diasAnticipacionAviso);
            }
        }

        [NotMapped]
        public bool NotificarRegistroPorVencer
        {
            get
            {
                //DateTime fechaInicioControlVencimientos = new DateTime(2016, 1, 15);
                return this.FechaInicioNoficacionVencimiento.HasValue && this.FechaInicioNoficacionVencimiento.Value >= DateTime.Now;
            }
        }

        [NotMapped]
        public TipoEliminacionEnum TipoEliminacionPermitida
        {
            get
            {
                bool puedeEliminar = false;

                if (HttpContext.Current.User.IsInRole("Administrador") || HttpContext.Current.User.IsInRole("InstructorExterno"))
                {
                    puedeEliminar = true;
                }
                else if (HttpContext.Current.User.IsInRole("InscripcionesExternas"))
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

        public bool EdicionDatosCapacitadoPermitida
        {
            get
            {
                if (HttpContext.Current.User.IsInRole("Administrador") || HttpContext.Current.User.IsInRole("InstructorExterno"))
                    return true;
                else if (HttpContext.Current.User.IsInRole("InscripcionesExternas"))
                    if (this.UsuarioModificacion == HttpContext.Current.User.Identity.Name && this.Estado == EstadosRegistroCapacitacion.Inscripto)
                        return true;

                return false;
            }
        }

        [NotMapped]
        public bool EdicionDocumentacionAdicionalInscripcionPermitida
        {
            get
            {
                bool puedeEditar = false;

                if (HttpContext.Current.User.IsInRole("Administrador"))
                {
                    puedeEditar = true;
                }
                /*
                else if (HttpContext.Current.User.IsInRole("InscripcionesExternas"))
                {
                    if (this.UsuarioModificacion == HttpContext.Current.User.Identity.Name && this.Estado == EstadosRegistroCapacitacion.Inscripto)
                        puedeEditar = true;
                }
                */

                return puedeEditar;
            }
        }

        [NotMapped]
        public bool VisualizacionDocumentacionAdicionalInscripcionPermitida
        {
            get
            {
                return HttpContext.Current.User.IsInRole("InstructorExterno");
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
                // Usar método centralizado para cambiar estado (sin ejecutar acciones aquí, se hacen en el controller después del SaveChanges)
                this.CambiarEstado(estadoFinal, ejecutarAcciones: false);

                return true;
            }
            else
                return false;
        }

        public bool Calificar(bool aprobado)
        {
            if (!this.Jornada.Curso.EvaluacionConNota)
            {
                EstadosRegistroCapacitacion nuevoEstado = aprobado 
                    ? EstadosRegistroCapacitacion.Aprobado 
                    : EstadosRegistroCapacitacion.NoAprobado;
                
                // Usar método centralizado para cambiar estado (sin ejecutar acciones aquí, se hacen en el controller después del SaveChanges)
                this.CambiarEstado(nuevoEstado, ejecutarAcciones: false);

                return true;
            }
            else
                return false;
        }

        public void BorrarCalificacion()
        {
            this.Nota = 0;
            this.NotaPrevia = 0;
            // Usar método centralizado para cambiar estado
            this.CambiarEstado(EstadosRegistroCapacitacion.Inscripto, ejecutarAcciones: false);
        }

        [NotMapped]
        public bool ListoParaEnviarOVAL
        {
            get
            {
                return (this.Capacitado != null ? this.Capacitado.TipoDocumento.PermiteEnviosOVAL : true) && this.FueCalificado && (this.EnvioOVALEstado == EstadosEnvioOVAL.PendienteEnvio || this.EnvioOVALEstado == EstadosEnvioOVAL.Rechazado);
            }
        }

        [NotMapped]
        public string MensajeNoListoParaEnviarOVAL
        {
            get
            {
                if (!this.Capacitado.TipoDocumento.PermiteEnviosOVAL) return string.Format("El tipo de documento {0} no permite envíos a OVAL", this.Capacitado.TipoDocumento.Descripcion);
                if (!this.FueCalificado) return "El registro aún no fue calificado";

                return string.Empty;
            }
        }

        /// <summary>
        /// Obtiene el estado de la notificación de vencimiento asociada a este registro, si existe.
        /// </summary>
        [NotMapped]
        public EstadoNotificacionVencimiento? EstadoNotificacion
        {
            get
            {
                return this.NotificacionesVencimiento?.FirstOrDefault()?.Estado;
            }
        }

        /// <summary>
        /// Obtiene la descripción textual del estado de notificación.
        /// </summary>
        [NotMapped]
        public string EstadoNotificacionTexto
        {
            get
            {
                var notificacion = this.NotificacionesVencimiento?.FirstOrDefault();
                
                if (notificacion == null)
                    return "Sin notificación";

                switch (notificacion.Estado)
                {
                    case EstadoNotificacionVencimiento.NotificacionPendiente:
                        return "Pendiente";
                    case EstadoNotificacionVencimiento.Notificado:
                        return "Notificado";
                    case EstadoNotificacionVencimiento.NoNotificar:
                        return "No Notificar";
                    case EstadoNotificacionVencimiento.NoNotificarYaActualizado:
                        return "Ya Actualizado";
                    default:
                        return "Desconocido";
                }
            }
        }

        /// <summary>
        /// Indica si el registro tiene una notificación asociada.
        /// </summary>
        [NotMapped]
        public bool TieneNotificacion
        {
            get
            {
                return (this.NotificacionesVencimiento?.Count ?? 0) > 0;
            }
        }

        /// <summary>
        /// Cambia el estado del registro de capacitación y ejecuta las acciones correspondientes.
        /// MÉTODO CENTRALIZADO: Usar este método en lugar de asignar Estado directamente.
        /// </summary>
        /// <param name="nuevoEstado">El nuevo estado a asignar</param>
        /// <param name="ejecutarAcciones">Si es true, ejecuta las acciones asociadas al cambio de estado (notificaciones, etc.)</param>
        public void CambiarEstado(EstadosRegistroCapacitacion nuevoEstado, bool ejecutarAcciones = true)
        {
            this.Estado = nuevoEstado;

            if (!ejecutarAcciones)
                return;

            // Ejecutar acciones específicas según el nuevo estado
            switch (nuevoEstado)
            {
                case EstadosRegistroCapacitacion.Aprobado:
                    EjecutarAccionesAlAprobar();
                    break;

                case EstadosRegistroCapacitacion.NoAprobado:
                    // Aquí se pueden agregar acciones específicas para cuando se marca como No Aprobado
                    // Por ahora no se requieren acciones adicionales
                    break;

                case EstadosRegistroCapacitacion.Inscripto:
                    // Aquí se pueden agregar acciones específicas para inscripciones
                    // Por ahora no se requieren acciones adicionales
                    break;
            }
        }

        /// <summary>
        /// Ejecuta las acciones necesarias cuando un registro es aprobado.
        /// Centraliza toda la lógica de negocio relacionada con la aprobación.
        /// </summary>
        private void EjecutarAccionesAlAprobar()
        {
            try
            {
                // Solo ejecutar si el registro tiene ID (ya fue guardado en BD)
                if (this.RegistroCapacitacionID == 0)
                    return;

                // Importar el controller de notificaciones
                // NOTA: Idealmente esto debería estar en una capa de servicios, 
                // pero por ahora mantenemos la estructura existente
                var notificacionesController = new Controllers.NotificacionesVencimientosController();

                // 1. Crear notificación de vencimiento para este registro
                notificacionesController.CrearNotificacionVencimiento(this.RegistroCapacitacionID);

                // 2. Actualizar notificaciones obsoletas de registros anteriores del mismo curso
                if (this.Jornada != null && this.Capacitado != null)
                {
                    notificacionesController.ActualizarNotificacionesObsoletasPorCapacitado(
                        this.CapacitadoID,
                        this.Jornada.CursoId,
                        this.Jornada.Fecha
                    );
                }
            }
            catch (Exception ex)
            {
                // Log del error pero no fallar la aprobación
                System.Diagnostics.Debug.WriteLine($"Error al ejecutar acciones de aprobación para registro {this.RegistroCapacitacionID}: {ex.Message}");
                
                // Opcionalmente, se podría registrar en un log más robusto
                // LogHelper.GetInstance().WriteMessage("registros", $"Error en aprobación: {ex.Message}");
            }
        }
    }
}