using System.ComponentModel.DataAnnotations;

namespace HareIsle.Test.Assets
{
    /// <summary>
    /// Attribute restricticted object.
    /// </summary>
    /// <remarks>Validation is done using attributes. Only values in the range 0-3 are allowed for <see cref="FirstNumber"/> property.</remarks>
    internal class AttrRestrictObject : IValidatableObject
    {
        [Range(0, 3)]
        public int FirstNumber { get; set; } = 0;

        public int SecondNumber { get; set; } = 0;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Enumerable.Empty<ValidationResult>();
    }
}