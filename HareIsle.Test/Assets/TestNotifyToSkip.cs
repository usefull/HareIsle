using System.ComponentModel.DataAnnotations;

namespace HareIsle.Test.Assets
{
    /// <summary>
    /// Message object for broadcast testing.
    /// </summary>
    internal class TestNotifyToSkip : IValidatableObject
    {
        public int Value { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return new List<ValidationResult>();
        }
    }
}