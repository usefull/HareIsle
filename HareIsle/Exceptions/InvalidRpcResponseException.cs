using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    public class InvalidRpcResponseException : Exception
    {
        public InvalidRpcResponseException() : base(Errors.InvalidRpcResponse)
        {
        }

        public InvalidRpcResponseException(string message) : base(message)
        {
        }

        public InvalidRpcResponseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidRpcResponseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
