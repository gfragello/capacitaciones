using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using Cursos.Models.Enums;

namespace Cursos.Models
{
    public class Jornada : ElementoAccesoControlado
    {
        public int JornadaID { get; set; }

        [Required(ErrorMessage = "Debe ingresar la fecha del curso")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime Fecha { get; set; }

        [Required(ErrorMessage = "Debe ingresar la Hora de la Jornada")]
        [RegularExpression(@"([01]?[0-9]|2[0-3]):[0-5][0-9]", ErrorMessage = "Hora no válida.")]
        public string Hora { get; set; }

        [NotMapped]
        public string HoraSinSeparador
        {
            get
            {
                string horaSinSeparador = this.Hora.Remove(this.Hora.IndexOf(':'));
                horaSinSeparador += this.Hora.Substring(this.Hora.IndexOf(':') + 1, 2);

                return horaSinSeparador;
            }
        }

        public int HoraFormatoNumerico { get; set; }
        
        public int Hora_HH
        {
            get
            {
                if (this.HoraFormatoNumerico > 0)
                    return this.HoraFormatoNumerico.ToString().Length == 3 ? int.Parse(this.HoraFormatoNumerico.ToString().Substring(0, 1)) : int.Parse(this.HoraFormatoNumerico.ToString().Substring(0, 2));

                return 0;
            }
        }

        public int Minuto_MM
        {
            get
            {
                if (this.HoraFormatoNumerico > 0)
                    return this.HoraFormatoNumerico.ToString().Length == 3 ? int.Parse(this.HoraFormatoNumerico.ToString().Substring(1, 2)) : int.Parse(this.HoraFormatoNumerico.ToString().Substring(2, 2));

                return 0;
            }
        }

        [NotMapped]
        public DateTime FechaHora
        {
            get
            {
                return new DateTime(this.Fecha.Year, this.Fecha.Month, this.Fecha.Day, this.Hora_HH, this.Minuto_MM, 0);
            }
        }

        [Required(ErrorMessage = "Debe seleccionar el lugar donde se desarrollará la jornada")]
        [Display(Name = "Lugar")]
        public int LugarID { get; set; }
        public virtual Lugar Lugar { get; set; }

        [Display(Name = "Dirección")]
        public string Direccion { get; set; }

        [Display(Name = "Curso")]
        public int CursoId { get; set; }
        public virtual Curso Curso { get; set; }

        [Display(Name = "Instructor")]
        public int InstructorId { get; set; }
        public virtual Instructor Instructor { get; set; }

        [Display(Name = "Características")]
        public string Caracteristicas { get; set; }

        public virtual List<RegistroCapacitacion> RegistrosCapacitacion { get; set; }

        [Display(Name = "Acta")]
        public int? PathArchivoID { get; set; }
        public virtual PathArchivo PathArchivo { get; set; }

        [Display(Name = "Cupos Disponibles")]
        [Required(AllowEmptyStrings = false)]
        public bool CuposDisponibles { get; set; }

        [Display(Name = "Tiene mínimo de asistentes")]
        public bool TieneMinimoAsistentes { get; set; }

        [Required(ErrorMessage = "Debe ingresar la {0}")]
        [Display(Name = "Cantidad mínima de asistentes")]
        public int MinimoAsistentes { get; set; }

        [Display(Name = "Tiene máximo de asistentes")]
        public bool TieneMaximoAsistentes { get; set; }

        [Required(ErrorMessage = "Debe ingresar la {0}")]
        [Display(Name = "Cantidad máxima de asistentes")]
        public int MaximoAsistentes { get; set; }

        [Display(Name = "Tiene Cierre Incripción")]
        public bool TieneCierreIncripcion { get; set; }

        [Required(ErrorMessage = "Debe ingresar las {0}")]
        [Display(Name = "Horas previas para el cierre de incripción")]
        public int HorasCierreInscripcion { get; set; }

        //TODO - GF 20190512 - Determinar si estas propiedades pueden incluirse en ElementoAccesoControlado
        public bool RequiereAutorizacion { get; set; }

        public string UsuarioCreacion { get; private set; }
        public DateTime? FechaCreacion { get; private set; }

        public string UsuarioAutorizacion { get; private set; }
        public DateTime? FechaAutorizacion { get; private set; }

        [Display(Name = "Permite inscripciones externas")]
        public bool PermiteInscripcionesExternas { get; set; }

        [Display(Name = "Permite envíos OVAL")]
        public bool PermiteEnviosOVAL { get; set; }

        public void IniciarAtributosAutorizacion(bool requiereAutorizacion)
        {
            this.RequiereAutorizacion = requiereAutorizacion;

            if (requiereAutorizacion)
            {
                this.Autorizada = false; //las jornadas que requieren autorización se inicializan como "No autorizadas"
                this.UsuarioCreacion = HttpContext.Current.User.Identity.Name;
                this.FechaCreacion = DateTime.Now;
            }
            else
            {
                this.Autorizada = true; //las jornadas que NO requieren autorización se inicializan como "Autorizadas"
                this.UsuarioCreacion = null;
                this.FechaCreacion = null;
            }

            this.UsuarioAutorizacion = null;
            this.FechaAutorizacion = null;
        }

        public bool OperacionesHabilitadas()
        {
            //si la jornada no requiere autorización, se permite trabajar sobre ella en cualquier caso
            if (!this.RequiereAutorizacion)
            {
                return true;
            }
            else
            {
                //si requiere autorización pero ya fue habilitada por algún usuario
                if (this.UsuarioAutorizacion != null)
                    return true;
            }

            return false;
        }

        // /////////////////// ///////////////////// ////////////////////// ////////////////////////

        [NotMapped]
        public string CuposDisponiblesTexto
        {
            get
            {
                if (this.CuposDisponibles)
                    return "Cupos disponibles";
                else
                    return "Sin cupos disponibles";
            }
        }

        /// ////////////////////////////////

        [NotMapped]
        public int CantidadCuposDisponibles
        {
            get
            {
                if (this.TieneMaximoAsistentes)
                    return this.MaximoAsistentes - this.TotalInscriptos;

                return 0;
            }
        }

        [NotMapped]
        public string CantidadCuposDisponiblesTexto
        {
            get
            {
                if (this.TieneMaximoAsistentes)
                {
                    if (this.QuedanCuposDisponibles)
                    {
                        return string.Format("Cupos disponibles: {0}", this.CantidadCuposDisponibles.ToString());
                    }
                    else
                        return "Sin cupos disponibles";
                }

                return "La jornada no tiene un cupo definido";
            }
        }

        public bool QuedanCuposDisponibles
        {
            get
            {
                if (this.TieneMaximoAsistentes)
                    if (this.CantidadCuposDisponibles == 0)
                        return false;

                return true;
            }
        }

        [NotMapped]
        public DateTime FechaCierreInscripcion
        {
            get
            {
                return this.FechaHora.AddHours(this.HorasCierreInscripcion * -1);
            }
        }

        [NotMapped]
        public string JornadaIdentificacionCompleta
        {
            get
            {
                return String.Format("{0} - {1} {2} - {3}", Curso.Descripcion, this.Fecha.ToShortDateString(), this.Hora, this.Lugar.AbrevLugar);
            }
        }

        [NotMapped]
        public string FechaHoraTexto
        {
            get
            {
                return String.Format("{0} {1}", this.Fecha.ToShortDateString(), this.Hora);
            }
        }

        [NotMapped]
        public string FechaFormatoYYYYYMMDD
        {
            get
            {
                return String.Format("{0}{1}{2}", this.Fecha.Year.ToString(), this.Fecha.Month.ToString().PadLeft(2, '0'), this.Fecha.Day.ToString().PadLeft(2, '0'));
            }
        }

        public bool Autorizada { get; set; }

        [NotMapped]
        public string AutorizadaTexto
        {
            get
            {
                if (this.Autorizada)
                {
                    return string.Format("Autorizada por {0} el {1}", this.UsuarioAutorizacion, this.FechaAutorizacion);
                }
                else
                {
                    return "Autorización pendiente";
                }
            }
        }

        [NotMapped]
        public string CreadaTexto
        {
            get
            {
                return string.Format("Creada por {0} el {1}", this.UsuarioCreacion, this.FechaCreacion);
            }
        }

        public int TotalInscriptos
        {
            get
            {
                return this.RegistrosCapacitacion != null ? this.RegistrosCapacitacion.Count() : 0;
            }
        }

        public string TotalInscriptosTexto
        {
            get
            {
                if (this.TotalInscriptos > 0)
                    return string.Format("Total de Inscriptos: {0}", this.RegistrosCapacitacion.Count().ToString());
                else
                    return "Sin incriptos";
            }
        }

        public string CierreIncripcionTexto
        {
            get
            {
                if (this.TieneCierreIncripcion)
                {
                    if (this.FechaCierreInscripcion >= DateTime.Now)
                        return string.Format("Cierre de inscripciones: {0} {1}", this.FechaCierreInscripcion.ToShortDateString(), this.FechaCierreInscripcion.ToShortTimeString());
                    else
                        return "Incripciones cerradas";
                }
                else
                    return "La jornada no tiene definido un plazo de cierre de inscripciones";
            }
        }

        public bool InscripcionesAbiertas
        {
            get
            {
                if (this.TieneCierreIncripcion)
                    return this.FechaCierreInscripcion >= DateTime.Now;

                return true;
            }
        }


        public string MinimoAsistentesTexto
        {
            get
            {
                if (this.TieneMinimoAsistentes)
                    return string.Format("Requiere un mínimo de {0} asistentes", this.MinimoAsistentes.ToString());
                else
                    return "La jornada no tiene definido un mínimo de asistentes";
            }
        }

        public bool PuedeRecibirIncripciones
        {
            get
            {
                return this.InscripcionesAbiertas && this.QuedanCuposDisponibles;
            }
        }

        //verifica si la jornada está en condiciones de recibir el acta y si el usuario actual está autorizado para hacerlo
        public bool PuedeAgregarActaUsuarioActual
        {
            get
            {
                return this.PuedeModificarse() && this.Autorizada;
            }
        }

        //verifica si la jornada está en condiciones de recibir incripciones y si el usuario actual está autorizado para hacerlo
        public bool PuedeAgregarInscripcionesUsuarioActual
        {
            get
            {
                return this.PuedeRecibirIncripciones && (this.PuedeModificarse() || HttpContext.Current.User.IsInRole("IncripcionesExternas"));
            }
        }

        public DateTime ObtenerFechaVencimiento(bool copiaJornada = false)
        {
            //TODO: hacer que esto sea configurable
            //que se pueda especificar en el curso que vence el último día del año
            //Tener en cuenta que al usar esta función desde la funcionalidad Copiar Jornada, this.Curso
            //no estará instanciado
            if (this.CursoId == 2)
                return new DateTime(this.Fecha.Year, 12, 31);
            else
            {
                if (!copiaJornada)
                    return new DateTime(this.Fecha.Year + this.Curso.Vigencia, this.Fecha.Month, this.Fecha.Day);
                else
                    return new DateTime(this.Fecha.Year + 3, this.Fecha.Month, this.Fecha.Day);
            }
        }

        public void ToggleAutorizada()
        {
            if (!this.Autorizada)
            {
                this.Autorizada = true;
                this.UsuarioAutorizacion = HttpContext.Current.User.Identity.Name;
                this.FechaAutorizacion = DateTime.Now;
            }
            else
            {
                this.Autorizada = false;
                this.UsuarioAutorizacion = null;
                this.FechaAutorizacion = null;
            }
        }
    }
}