using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    public class RpcResponseDeserializationException : Exception
    {
        public RpcResponseDeserializationException() : base(Errors.RpcResponseDeserializationError)
        {
        }

        public RpcResponseDeserializationException(string message) : base(message)
        {
        }

        public RpcResponseDeserializationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RpcResponseDeserializationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}