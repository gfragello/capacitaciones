using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

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

        [Display(Name = "Departamento")]
        public int DepartamentoID { get; set; }
        public virtual Departamento Departamento { get; set; }

        public string Localidad { get; set; }

        [Display(Name = "Código Postal")]
        public string CodigoPostal { get; set; }

        //[EmailAddress(ErrorMessage = "La dirección de Email ingresada no es válida")]
        [MultipleMailValidator]
        public string Email { get; set; }

        public virtual List<Capacitado> Capacitados { get; set; }

        public virtual List<EmpresaUsuario> Usuarios { get; set; }
    }
}