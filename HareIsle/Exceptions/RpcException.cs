using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    /// <summary>
    /// The exception that is throw when RPC handling error occurs.
    /// </summary>
    public class RpcException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public RpcException() : base(Errors.UnknownRpcError)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error messages.</param>
        public RpcException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error messages.</param>
        /// <param name="innerException">The exception that caused this error.</param>
        public RpcException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Serialization context.</param>
        protected RpcException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}