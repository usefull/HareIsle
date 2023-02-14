using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    /// <summary>
    /// The exception that is throw by <see cref="RpcHandler{TRequest, TResponse}"/> when an error occurs during the deserialization of the response.
    /// </summary>
    public class RpcResponseDeserializationException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public RpcResponseDeserializationException() : base(Errors.RpcResponseDeserializationError)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error messages.</param>
        public RpcResponseDeserializationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error messages.</param>
        /// <param name="innerException">The exception that caused this error.</param>
        public RpcResponseDeserializationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Serialization context.</param>
        protected RpcResponseDeserializationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}