using HareIsle.Resources;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace HareIsle.Entities
{
    /// <summary>
    /// A wrapper object for messages exchanged between actors.
    /// </summary>
    /// <typeparam name="TPayload">Payload object type.</typeparam>
    public class Message<TPayload> : IValidatableObject
        where TPayload : class, IValidatableObject
    {
        /// <summary>
        /// A full name of the payload object type.
        /// </summary>
        [Required]
        public string? Type { get; set; }

        /// <summary>
        /// A payload message object.
        /// </summary>
        [Required]
        public TPayload? Payload { get; set; }

        /// <summary>
        /// Serializes the object.
        /// </summary>
        /// <returns>An array of bytes representing a UTF-8 string containing a JSON object.</returns>
        /// <exception cref="SerializationException">In case of serializing error.</exception>
        public byte[] ToBytes()
        {
            if (Payload == null)
                throw new SerializationException(Errors.NullPayload);

            try
            {
                Type = Payload.GetType().AssemblyQualifiedName;
                var serialized = JsonConvert.SerializeObject(this);
                return Encoding.UTF8.GetBytes(serialized);
            }
            catch (Exception ex)
            {
                throw new SerializationException(Errors.SerializingError, ex);
            }
        }

        /// <summary>
        /// Deserializes the object.
        /// </summary>
        /// <param name="bytes">An array of bytes representing a UTF-8 string containing a JSON object.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="SerializationException">In case of deserializing error.</exception>
        public static Message<TPayload> FromBytes(byte[] bytes)
        {
            try
            {
                var msg = Encoding.UTF8.GetString(bytes);
                return JsonConvert.DeserializeObject<Message<TPayload>>(msg)!;
            }
            catch (Exception ex)
            {
                throw new SerializationException(Errors.SerializingError, ex);
            }
        }

        /// <summary>
        /// Validates the object and throws an exception if the oobject is invalid.
        /// </summary>
        /// <exception cref="ValidationException">In case of the object is invalid.</exception>
        public void ThrowIfInvalid()
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(this, new ValidationContext(this), results, true))
            {
                if (results.Any())
                    throw new ValidationException(results.First(), null, this);
                else
                    throw new ValidationException(Errors.UnknownValidationError);
            }
        }

        #region IValidatableObject interface implementation

        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="validationContext">Validation context.</param>
        /// <returns>Validation errors.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Type != Payload!.GetType().AssemblyQualifiedName)
                return new[] { new ValidationResult(Errors.PayloadTypeMismatch, new[] { nameof(Payload) }) };

            var results = new List<ValidationResult>();
            Validator.TryValidateObject(Payload!, new ValidationContext(Payload!), results, true);
            return results;
        }

        #endregion
    }
}