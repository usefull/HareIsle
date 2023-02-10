using HareIsle.Resources;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace HareIsle.Entities
{
    /// <summary>
    /// RPC response object.
    /// </summary>
    /// <typeparam name="TPayload">Payload object type.</typeparam>
    internal class RpcResponse<TPayload> : IValidatableObject
        where TPayload : class, IValidatableObject
    {
        /// <summary>
        /// Payload object.
        /// </summary>
        public TPayload? Payload { get; set; }

        /// <summary>
        /// Error description that occurred during the RPC processing.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Implements <see cref="IValidatableObject"/> interface to validate the object.
        /// </summary>
        /// <param name="validationContext">Validation context.</param>
        /// <returns>Validation results.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if ((Payload == null && string.IsNullOrWhiteSpace(Error)) || (Payload != null && !string.IsNullOrWhiteSpace(Error)))
                return Enumerable.Repeat(new ValidationResult(Errors.PayloadAndErrorCantAtTheSameTime, new[] { nameof(Payload), nameof(Error) }), 1);

            if (Payload == null)
                return Enumerable.Empty<ValidationResult>();

            var context = new ValidationContext(Payload);
            var payloadResults = new List<ValidationResult>();
            if (Validator.TryValidateObject(Payload, context, payloadResults, true))
                return Enumerable.Empty<ValidationResult>();

            return payloadResults.Select(r =>
            {
                r.ErrorMessage = $"{Errors.InvalidPayload}{r.ErrorMessage}";
                return r;
            });
        }
    }
}