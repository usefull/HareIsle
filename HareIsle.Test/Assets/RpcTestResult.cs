using System.ComponentModel.DataAnnotations;

namespace HareIsle.Test.Assets
{
    internal class RpcTestResult : IValidatableObject
    {
        public int ResultNumber { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Enumerable.Empty<ValidationResult>();
    }
}