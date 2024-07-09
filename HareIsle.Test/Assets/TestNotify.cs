using System.ComponentModel.DataAnnotations;

namespace HareIsle.Test.Assets
{
    /// <summary>
    /// Message object for broadcast tesing.
    /// </summary>
    internal class TestNotify : IValidatableObject
    {
        public string Message { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Enumerable.Empty<ValidationResult>();
    }
}