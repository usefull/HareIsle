using System;
using System.ComponentModel.DataAnnotations;

namespace HareIsle.EventArgs
{
    public class SendResponseErrorEventArgs<TResponse> : System.EventArgs
        where TResponse : class, IValidatableObject
    {
        public SendResponseErrorEventArgs(TResponse response, Exception exception)
        {
            Response = response;
            Exception = exception;
        }

        public TResponse Response { get; }

        public Exception Exception { get; }
    }
}