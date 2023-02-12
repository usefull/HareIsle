using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    public class RpcRequestSerializationException : Exception
    {
        public RpcRequestSerializationException() : base(Errors.RpcRequestSerializationError)
        {
        }

        public RpcRequestSerializationException(string message) : base(message)
        {
        }

        public RpcRequestSerializationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RpcRequestSerializationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
