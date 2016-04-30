using Cursos.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Cursos.Models
{
    public class CIValidator : ValidationAttribute
    {
        private ValidationHelper validationHelper = ValidationHelper.GetInstance();

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value != null)
            {
                PropertyInfo tdPropertyInfo = validationContext.ObjectType.GetProperty("TipoDocumentoID");
                object tdPropertyValue = tdPropertyInfo.GetValue(validationContext.ObjectInstance, null);

                if ((int)tdPropertyValue == 1) //Solo se valida la CI si el tipo de documento que se seleccionó
                {
                    string ci = value.ToString();

                    if (validationHelper.ValidateCI(ci))
                    {
                        return ValidationResult.Success;
                    }
                    else
                    {
                        return new ValidationResult("La cédula de identidad ingresada no se válida");
                    }
                }
                else
                {
                    return ValidationResult.Success;
                }
            }
            else
            {
                return new ValidationResult("Debe ingresar un cédula de identidad");
            }
        }

    }
}