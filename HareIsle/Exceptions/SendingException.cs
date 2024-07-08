using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    /// <summary>
    /// Throws in case of a message sending error.
    /// </summary>
    public class SendingException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SendingException() : base(Errors.SendingError) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error message.</param>
        public SendingException(string message) : base(message) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">The exception that caused the error.</param>
        public SendingException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization params.</param>
        /// <param name="context">Serialization context.</param>
        protected SendingException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}