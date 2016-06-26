using Cursos.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Cursos.Models
{
    public class MultipleMailValidator : ValidationAttribute
    {
        ValidationHelper validationHelper = ValidationHelper.GetInstance();

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value != null)
            {
                string multipleMail = value.ToString();

                if (validationHelper.ValidateMultipleEmails(multipleMail))
                {
                    return ValidationResult.Success;
                }
                else
                {
                    return new ValidationResult("Email no válido. Si está ingresando más de una dirección, debe serpararlas con coma (,)");
                }
            }

            return ValidationResult.Success;
        }
    }
}