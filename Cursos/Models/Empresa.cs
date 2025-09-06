using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.WebPages;

namespace Cursos.Models
{
    //CONS: Consultar cuales son los valores obligatorio para esta y para el resto de las clases
    public class Empresa : ElementoAccesoControlado
    {
        public int EmpresaID { get; set; }

        [Required(ErrorMessage = "Debe ingresar el {0}")]
        [Display(Name = "Nombre de fantasía")]
        public String NombreFantasia { get; set; }

        public string Domicilio { get; set; }

        //La Razón Social no puede ser obligatoria porque algunas Empresas del archivo Excel no tienen este datos
        //[Required(ErrorMessage = "Debe ingresar la {0}")]
        [Display(Name = "Razón Social")]
        public string RazonSocial { get; set; }

        //El RUT no puede ser obligatorio porque algunas Empresas del archivo Excel no tienen este datos
        //[Required(ErrorMessage = "Debe ingresar el {0}")]
        public string RUT { get; set; }

        public string NombreCompleto
        {
            get
            {
                if (RUT.IsEmpty() || RazonSocial.IsEmpty())
                    return NombreFantasia;

                return String.Format("{0} / {1}", RazonSocial, RUT);
            }
        }

        [Display(Name = "Departamento")]
        public int DepartamentoID { get; set; }
        public virtual Departamento Departamento { get; set; }

        public string Localidad { get; set; }

        [Display(Name = "Código Postal")]
        public string CodigoPostal { get; set; }

        //[EmailAddress(ErrorMessage = "La dirección de Email ingresada no es válida")]
        [MultipleMailValidator]
        public string Email { get; set; }

        [Display(Name = "Email Facturación")]
        public string EmailFacturacion { get; set; }

        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [Display(Name = "Régimen de Pago")]
        [Required(ErrorMessage = "Debe seleccionar un régimen de pago")]
        public int RegimenPagoID { get; set; } = 1; // Valor por defecto: Régimen Estándar
        public virtual RegimenPago RegimenPago { get; set; }

        public virtual List<Capacitado> Capacitados { get; set; }

        public virtual List<EmpresaUsuario> Usuarios { get; set; }
    }
}