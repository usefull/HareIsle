using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    public class RpcException : Exception
    {
        public RpcException() : base(Errors.UnknownRpcError)
        {
        }

        public RpcException(string message) : base(message)
        {
        }

        public RpcException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RpcException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}