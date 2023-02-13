using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    public class RpcProtocolException : Exception
    {
        public RpcProtocolException() : base(Errors.RpcProtocolError)
        {
        }

        public RpcProtocolException(string message) : base(message)
        {
        }

        public RpcProtocolException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RpcProtocolException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
