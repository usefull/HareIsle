using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    /// <summary>
    /// The exception that is throw when <see cref="RpcHandler{TRequest, TResponse}"/> recieves invalid request.
    /// </summary>
    public class InvalidRpcRequestException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public InvalidRpcRequestException() : base(Errors.InvalidRpcRequest)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error messages.</param>
        public InvalidRpcRequestException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error messages.</param>
        /// <param name="innerException">The exception that caused this error.</param>
        public InvalidRpcRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Serialization context.</param>
        protected InvalidRpcRequestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}