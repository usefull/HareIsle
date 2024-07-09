using System;
using System.Runtime.Serialization;
using HareIsle.Resources;

namespace HareIsle.Exceptions
{
    /// <summary>
    /// Throws when an error occurs when trying to subscribe to any messages.
    /// </summary>
    public class SubscriptionException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SubscriptionException() : base(Errors.SubscriptionError) { }

        /// <summary>
        /// Costructor.
        /// </summary>
        /// <param name="message">The error message.</param>
        public SubscriptionException(string? message) : base(message) { }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public SubscriptionException(string? message, Exception? innerException) : base(message, innerException) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization parameters.</param>
        /// <param name="context">The serializing context.</param>
        protected SubscriptionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}