using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    public class InvalidRpcRequestException : Exception
    {
        public InvalidRpcRequestException() : base(Errors.InvalidRpcRequest)
        {
        }

        public InvalidRpcRequestException(string message) : base(message)
        {
        }

        public InvalidRpcRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidRpcRequestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}