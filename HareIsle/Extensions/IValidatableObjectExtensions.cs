using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace HareIsle.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IValidatableObject"/>.
    /// </summary>
    internal static class IValidatableObjectExtensions
    {
        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="obj">Object to validate.</param>
        /// <param name="errors">Accepts the validation error message if the object is invalid.</param>
        /// <returns>True if the object is valid, otherwise - false.</returns>
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