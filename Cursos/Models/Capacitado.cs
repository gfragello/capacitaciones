using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Cursos.Models.Enums;
using Cursos.Helpers;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace Cursos.Models
{
    [Table("Capacitados")]
    public class Capacitado : ElementoAccesoControlado
    {
        public int CapacitadoID { get; set; }

        private string nombre;
        [Required(ErrorMessage = "Debe ingresar el nombre del capacitado")]
        public string Nombre
        {
            get { if (nombre != null) return nombre.ToUpper(); else return string.Empty; }
            set { nombre = value; }
        }

        private string apellido;
        [Required(ErrorMessage = "Debe ingresar el apellido del capacitado")]
        public string Apellido
        {
            get { if (apellido != null) return apellido.ToUpper(); else return string.Empty; }
            set { apellido = value; }
        }


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

        public bool CargarFoto(HttpPostedFileBase foto)
        {
            if (foto != null && foto.ContentLength > 0)
            {

                string nombreArchivo = PathArchivoHelper.GetInstance().ObtenerNombreFotoCapacitado(this.CapacitadoID,
                                                                                                   System.IO.Path.GetExtension(foto.FileName));

                string carpetaArchivo = PathArchivoHelper.GetInstance().ObtenerCarpetaFotoCapacitado(this.CapacitadoID);

                string pathDirectorio = Path.Combine(HttpContext.Current.Server.MapPath("~/Images/FotosCapacitados/"), carpetaArchivo);

                this.PathArchivo = PathArchivoHelper.GetInstance().ObtenerPathArchivo(nombreArchivo,
                                                                                      carpetaArchivo,
                                                                                      pathDirectorio,
                                                                                      foto,
                                                                                      TiposArchivo.FotoCapacitado);

                var pathArchivoImagen = Path.Combine(pathDirectorio, nombreArchivo);

                Stream streamSinEXIF = new MemoryStream();

                using (Image imgOriginal = Image.FromFile(pathArchivoImagen))
                {
                    //rotate the picture by 90 degrees and re-save the picture as a Jpeg
                    //imgOriginal.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    //img.Save(pathArchivoImagen, System.Drawing.Imaging.ImageFormat.Jpeg);

                    var streamOriginal = new MemoryStream();
                    imgOriginal.Save(streamOriginal, ImageFormat.Jpeg);

                    // If you're going to read from the stream, you may need to reset the position to the start
                    streamOriginal.Position = 0;

                    streamSinEXIF = ImageHelper.GetInstance().PatchAwayExif(streamOriginal, streamSinEXIF);
                }

                Image imgSinEXIF = Image.FromStream(streamSinEXIF);

                //si la imagen está apaisada, rotarla 90 grados
                //20201003 - GF: hasta que se resuelva el cropping de las fotos no se capturan a través de la cámara
                //               y no es neceario rotarla
                /*
                if (imgSinEXIF.Width > imgSinEXIF.Height)
                    imgSinEXIF.RotateFlip(RotateFlipType.Rotate90FlipNone);
                */

                imgSinEXIF.Save(pathArchivoImagen, System.Drawing.Imaging.ImageFormat.Jpeg);

                return true;
            }

            return false;
        }

        public bool RotarFoto(string direccion)
        {
            if (this.PathArchivo != null)
            {
                string carpetaArchivo = PathArchivoHelper.GetInstance().ObtenerCarpetaFotoCapacitado(this.CapacitadoID);
                string pathDirectorio = Path.Combine(HttpContext.Current.Server.MapPath("~/Images/FotosCapacitados/"), carpetaArchivo);

                var pathArchivoImagen = Path.Combine(pathDirectorio, this.PathArchivo.NombreArchivo);

                using (Image img = Image.FromFile(pathArchivoImagen))
                {
                    //rotate the picture by 90 degrees and re-save the picture as a Jpeg

                    if (direccion == "i") //si se va a rotar a la izquierda
                        img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    else if (direccion == "d")
                        img.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    else
                        return false;

                    img.Save(pathArchivoImagen, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                return true;
            }
            return false;
        }

    }
}