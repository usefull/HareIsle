using System.ComponentModel.DataAnnotations;

namespace HareIsle.Test.Assets
{
    /// <summary>
    /// The message for queue handling tests.
    /// </summary>
    public class TestQueueMessage : IValidatableObject
    {
        public string Text { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Enumerable.Empty<ValidationResult>();
    }
}