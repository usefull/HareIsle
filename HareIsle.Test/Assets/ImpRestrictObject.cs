using System.ComponentModel.DataAnnotations;

namespace HareIsle.Test.Assets
{
    /// <summary>
    /// Imperative restricticted object.
    /// </summary>
    /// <remarks>Validation performs in <see cref="Validate(ValidationContext)"/> method. Only positive values are alowed for <see cref="Value"/> property.</remarks>
    internal class ImpRestrictObject : IValidatableObject
    {
        public int Value { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Value < 0)
                return new List<ValidationResult> { new ("Only positive values are alowed", new[] { nameof(Value) }) };

            return new List<ValidationResult>();
        }
    }
}