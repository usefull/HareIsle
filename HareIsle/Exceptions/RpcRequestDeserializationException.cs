using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    public class RpcRequestDeserializationException : Exception
    {
        public RpcRequestDeserializationException() : base(Errors.RpcRequestDeserializationError)
        {
        }

        public RpcRequestDeserializationException(string message) : base(message)
        {
        }

        public RpcRequestDeserializationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RpcRequestDeserializationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
