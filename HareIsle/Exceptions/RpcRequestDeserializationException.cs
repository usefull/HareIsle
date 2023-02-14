using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    /// <summary>
    /// The exception that is throw by <see cref="RpcHandler{TRequest, TResponse}"/> when an error occurs during the deserialization of the request.
    /// </summary>
    public class RpcRequestDeserializationException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public RpcRequestDeserializationException() : base(Errors.RpcRequestDeserializationError)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error messages.</param>
        public RpcRequestDeserializationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error messages.</param>
        /// <param name="innerException">The exception that caused this error.</param>
        public RpcRequestDeserializationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Serialization context.</param>
        protected RpcRequestDeserializationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}