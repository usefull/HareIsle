using System;

namespace HareIsle.EventArgs
{
    public class RequestHandlingErrorEventArgs<TRequest> : System.EventArgs
    {
        public RequestHandlingErrorEventArgs(TRequest request, Exception exception)
        {
            Request = request;
            Exception = exception;
        }

        public TRequest Request { get; }

        public Exception Exception { get; }
    }
}