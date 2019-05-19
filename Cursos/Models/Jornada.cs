﻿using System;
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

        //TODO - GF 20190512 - Determinar si estas propiedades pueden incluirse en ElementoAccesoControlado
        public bool RequiereAutorizacion { get; set; }

        public string UsuarioCreacion { get; private set; }
        public DateTime? FechaCreacion { get; private set; }

        public string UsuarioAutorizacion { get; private set; }
        public DateTime? FechaAutorizacion { get; private set; }

        public void IniciarAtributosAutorizacion(bool requiereAutorizacion)
        {
            this.RequiereAutorizacion = requiereAutorizacion;

            if (requiereAutorizacion)
            {
                this.UsuarioCreacion = HttpContext.Current.User.Identity.Name;
                this.FechaCreacion = DateTime.Now;
            }
            else
            {
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

        [NotMapped]
        public string JornadaIdentificacionCompleta
        {
            get
            {
                return String.Format("{0} - {1} {2} - {3}", Curso.Descripcion, this.Fecha.ToShortDateString(), this.Hora, this.Lugar.AbrevLugar);
            }
        }

        [NotMapped]
        public string FechaHora
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

        [NotMapped]
        public bool Autorizada
        {
            get
            {
                if (this.RequiereAutorizacion)
                    return this.UsuarioAutorizacion != null;

                return true; //las jornadas que no requieren autorización se consideran autorizadas
            }
        }

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
                this.UsuarioAutorizacion = HttpContext.Current.User.Identity.Name;
                this.FechaAutorizacion = DateTime.Now;
            }
            else
            {
                this.UsuarioAutorizacion = null;
                this.FechaAutorizacion = null;
            }
        }
    }
}