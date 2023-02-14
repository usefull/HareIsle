using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    /// <summary>
    /// The exception that is throw by <see cref="RpcClient"/> when an error occurs during the serialization of the request.
    /// </summary>
    public class RpcRequestSerializationException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public RpcRequestSerializationException() : base(Errors.RpcRequestSerializationError)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error messages.</param>
        public RpcRequestSerializationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error messages.</param>
        /// <param name="innerException">The exception that caused this error.</param>
        public RpcRequestSerializationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Serialization context.</param>
        protected RpcRequestSerializationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}