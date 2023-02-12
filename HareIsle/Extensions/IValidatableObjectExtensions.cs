using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace HareIsle.Extensions
{
    internal static class IValidatableObjectExtensions
    {
        public static bool IsValid(this IValidatableObject obj, out string errors)
        {
            errors = string.Empty;
            var results = new List<ValidationResult>();
            var isValid =  Validator.TryValidateObject(obj, new ValidationContext(obj), results, true);

            if (!isValid)
                errors = string.Join(". ", results.Select(r => $"{r.ErrorMessage} (members: {string.Join(", ", r.MemberNames)})"));

            return isValid;
        }            
    }
}