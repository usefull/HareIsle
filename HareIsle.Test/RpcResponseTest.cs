using HareIsle.Entities;
using System.ComponentModel.DataAnnotations;

namespace HareIsle.Test
{
    /// <summary>
    /// <see cref="RpcResponse{TPayload}"/> test cases.
    /// </summary>
    [TestClass]
    public class RpcResponseTest
    {
        /// <summary>
        /// Tests failed validation of inconsistent objects.
        /// </summary>
        [TestMethod]
        public void InconsistentRpcResponseValidationTest()
        {
            var respose = new RpcResponse<PayloadEmulator>();

            var results = new List<ValidationResult>();
            Assert.IsFalse(Validator.TryValidateObject(respose, new ValidationContext(respose), results));
            Assert.IsTrue(results.Count == 1);
            Assert.IsTrue(results[0].MemberNames.Count() == 2);
            Assert.IsTrue(results[0].MemberNames.Contains(nameof(respose.Payload)));
            Assert.IsTrue(results[0].MemberNames.Contains(nameof(respose.Error)));

            respose.Payload = new PayloadEmulator();
            respose.Error = "Error";

            results = new List<ValidationResult>();
            Assert.IsFalse(Validator.TryValidateObject(respose, new ValidationContext(respose), results));
            Assert.IsTrue(results.Count == 1);
            Assert.IsTrue(results[0].MemberNames.Count() == 2);
            Assert.IsTrue(results[0].MemberNames.Contains(nameof(respose.Payload)));
            Assert.IsTrue(results[0].MemberNames.Contains(nameof(respose.Error)));
        }

        /// <summary>
        /// Tests failed attribute validation of objects contains invalid payloads.
        /// </summary>
        [TestMethod]
        public void InvalidPayloadAttrValidationTest()
        {
            var response = new RpcResponse<PayloadEmulator>()
            {
                Payload = new PayloadEmulator
                {
                    Quantity = 5,
                    Title = "Title",
                }
            };
            var results = new List<ValidationResult>();
            Assert.IsFalse(Validator.TryValidateObject(response, new ValidationContext(response), results));
            Assert.IsTrue(results.Count == 1);
            Assert.IsTrue(results.Count(r => r.MemberNames.Contains(nameof(response.Payload.Description))) == 1);

            response.Payload.Quantity = 15;
            response.Payload.Description = "The payload description";
            results = new List<ValidationResult>();
            Assert.IsFalse(Validator.TryValidateObject(response, new ValidationContext(response), results));
            Assert.IsTrue(results.Count == 2);
            Assert.IsTrue(results.Count(r => r.MemberNames.Contains(nameof(response.Payload.Quantity))) == 1);
            Assert.IsTrue(results.Count(r => r.MemberNames.Contains(nameof(response.Payload.Description))) == 1);
        }

        /// <summary>
        /// Tests failed method validation of objects contains invalid payloads.
        /// </summary>
        [TestMethod]
        public void InvalidPayloadMethodValidationTest()
        {
            var response = new RpcResponse<PayloadEmulator>()
            {
                Payload = new PayloadEmulator
                {
                    Quantity = 5,
                    Description = "Here is stopword"
                }
            };
            var results = new List<ValidationResult>();
            Assert.IsFalse(Validator.TryValidateObject(response, new ValidationContext(response), results));
            Assert.IsTrue(results.Count == 2);
            Assert.IsTrue(results.Count(r => r.MemberNames.Contains(nameof(response.Payload.Title))) == 1);
            Assert.IsTrue(results.Count(r => r.MemberNames.Contains(nameof(response.Payload.Description))) == 1);
            Assert.IsTrue(results.Count(r => r.ErrorMessage?.Contains("stopword") ?? false) == 1);
            Assert.IsTrue(results.Count(r => r.ErrorMessage?.Contains("null") ?? false) == 1);
        }

        /// <summary>
        /// Tests successful validation.
        /// </summary>
        [TestMethod]
        public void SuccessRpcResponseValidationTest()
        {
            var response = new RpcResponse<PayloadEmulator>()
            {
                Payload = new PayloadEmulator
                {
                    Quantity = 5,
                    Title = "Title",
                    Description = "Description"
                }
            };
            var context = new ValidationContext(response);
            var results = new List<ValidationResult>();
            Assert.IsTrue(Validator.TryValidateObject(response, context, results));
        }
    }

    /// <summary>
    /// Payload emulation object using in tests.
    /// </summary>
    internal class PayloadEmulator : IValidatableObject
    {
        [Range(2, 9)]
        public int Quantity { get; set; }

        public string? Title { get; set; }

        [Required]
        [StringLength(20)]
        public string? Description { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Title))
                yield return new ValidationResult("null", new[] { nameof(Title) });

            if (Description?.Contains("stopword") ?? false)
                yield return new ValidationResult("stopword", new[] { nameof(Description) });
        }
    }

}
