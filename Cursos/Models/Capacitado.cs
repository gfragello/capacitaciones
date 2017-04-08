using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Cursos.Models.Enums;

namespace Cursos.Models
{
    [Table("Capacitados")]
    public class Capacitado : ElementoAccesoControlado
    {
        public int CapacitadoID { get; set; }

        [Required(ErrorMessage = "Debe ingresar el nombre del capacitado")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Debe ingresar el apellido del capacitado")]
        public string Apellido { get; set; }

        [NotMapped]
        public string NombreCompleto
        {
            get
            {
                return string.Format("{0} {1}", this.Nombre, this.Apellido);
            }
        }

        public int TipoDocumentoID { get; set; }
        public virtual TipoDocumento TipoDocumento { get; set; }

        [CIValidator]
        [Required(ErrorMessage = "Debe ingresar el documento del capacitado")]
        public String Documento { get; set; }

        [NotMapped]
        public string DocumentoCompleto
        {
            get
            {
                if (this.TipoDocumento.Abreviacion == "CI")
                {
                    string documentoSinDigitoVerificador = this.Documento.Substring(0, this.Documento.Length - 1);
                    string digitoVerificador = this.Documento.Substring(this.Documento.Length - 1, 1);

                    return String.Format("{0} {1}-{2}", this.TipoDocumento.Abreviacion, documentoSinDigitoVerificador, digitoVerificador);
                }
                else
                {
                    return String.Format("{0} {1}", this.TipoDocumento.Abreviacion, this.Documento);
                }
            }
        }

        [NotMapped]
        public string IdentificacionCompleta
        {
            get
            {
                return String.Format("{0} ({1})", this.NombreCompleto, this.DocumentoCompleto);
            }
        }

        [Display(Name = "Fecha de nacimiento")]
        public DateTime? Fecha { get; set; }

        [Display(Name = "Empresa")]
        public int EmpresaID { get; set; }
        public virtual Empresa Empresa { get; set; }

        [Display(Name = "Foto")]
        public int? PathArchivoID { get; set; }
        public virtual PathArchivo PathArchivo { get; set; }

        [NotMapped]
        public int Edad
        {
            get
            {
                return Fecha != null ? this.ObtenerEdadFecha(DateTime.Now) : 0;
            }
        }


        public int ObtenerEdadFecha(DateTime HastaFecha)
        {
            if (this.Fecha != null)
            {
                DateTime zeroTime = new DateTime(1, 1, 1);

                TimeSpan span = (DateTime)HastaFecha - (DateTime)this.Fecha;

                // Because we start at year 1 for the Gregorian calendar, we must subtract a year here.
                try
                {
                    //por la naturaleza del modelo de datos, es posible ingresar cualquier año
                    //por lo que sería posible que el año de nacimiento sea igual al de la 
                    //jornada dando por resulatado un error al hacer este cálculo
                    return (zeroTime + span).Year - 1;
                }
                catch (Exception)
                {
                    return 0;
                } 
            }

            return 0;
        }

        public List<RegistroCapacitacion> RegistrosCapacitacion { get; set; }

        public List<RegistroCapacitacion> UltimoRegistroCapacitacionPorCurso(int? CursoID, bool soloEvaluados = true)
        {
            List<RegistroCapacitacion> registrosCapacitacionRet = new List<RegistroCapacitacion>();

            var registrosCapacitacion = this.RegistrosCapacitacion;

            if (CursoID != null && CursoID != -1)
                registrosCapacitacion = registrosCapacitacion.Where(r => r.Jornada.CursoId == CursoID).ToList();

            if (soloEvaluados)
                registrosCapacitacion = registrosCapacitacion.Where(r => r.Estado == EstadosRegistroCapacitacion.Aprobado ||
                                                                         r.Estado == EstadosRegistroCapacitacion.NoAprobado).ToList();

            registrosCapacitacion = registrosCapacitacion.OrderBy(r => r.Jornada.CursoId).ThenByDescending(r => r.Jornada.Fecha).ToList();

            int cursoIdActual = -1;

            foreach (var r in registrosCapacitacion.ToList())
            {
                if (r.Jornada.CursoId != cursoIdActual)
                {
                    cursoIdActual = r.Jornada.CursoId;
                    registrosCapacitacionRet.Add(r);
                }
            }

            return registrosCapacitacionRet;
        }

        public List<RegistroCapacitacion> ObtenerRegistrosCapacitacionEvaluados()
        {
            List<RegistroCapacitacion> registrosCapacitacionEvaluados = null;

            if (this.RegistrosCapacitacion != null)
                registrosCapacitacionEvaluados =
                this.RegistrosCapacitacion.Where(r => r.Estado == EstadosRegistroCapacitacion.Aprobado ||
                                                      r.Estado == EstadosRegistroCapacitacion.NoAprobado).ToList();

            return registrosCapacitacionEvaluados;
        }

    }
}