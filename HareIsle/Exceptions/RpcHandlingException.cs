using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    /// <summary>
    /// Throws when RPC query handling error occurs.
    /// </summary>
    public class RpcHandlingException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public RpcHandlingException() : base(Errors.RpcHandlingError) { }

        /// <summary>
        /// Conctructor.
        /// </summary>
        /// <param name="message">The error message.</param>
        public RpcHandlingException(string? message) : base(message) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The exception that caused the error.</param>
        public RpcHandlingException(string? message, Exception? innerException) : base(message, innerException) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">The serialization context.</param>
        /// <param name="context">The streaming context.</param>
        protected RpcHandlingException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}