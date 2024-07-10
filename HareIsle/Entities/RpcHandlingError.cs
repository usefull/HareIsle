using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace HareIsle.Entities
{
    /// <summary>
    /// The RPC response in case of handling error.
    /// </summary>
    public class RpcHandlingError : IValidatableObject
    {
        /// <summary>
        /// The error message.
        /// </summary>
        [Required]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>The validation error list.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Enumerable.Empty<ValidationResult>();
    }
}