using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    /// <summary>
    /// The exception that is thrown by <see cref="RpcHandler{TRequest, TResponse}"/> when the RPC protocol is violated.
    /// </summary>
    public class RpcProtocolException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public RpcProtocolException() : base(Errors.RpcProtocolError)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error messages.</param>
        public RpcProtocolException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error messages.</param>
        /// <param name="innerException">The exception that caused this error.</param>
        public RpcProtocolException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Serialization context.</param>
        protected RpcProtocolException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}