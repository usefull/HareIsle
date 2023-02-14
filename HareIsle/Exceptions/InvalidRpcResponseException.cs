using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    /// <summary>
    /// The exception that is throw when <see cref="RpcClient"/> recieves invalid response as a result of RPC call.
    /// </summary>
    public class InvalidRpcResponseException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public InvalidRpcResponseException() : base(Errors.InvalidRpcResponse)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error messages.</param>
        public InvalidRpcResponseException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error messages.</param>
        /// <param name="innerException">The exception that caused this error.</param>
        public InvalidRpcResponseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Serialization context.</param>
        protected InvalidRpcResponseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}