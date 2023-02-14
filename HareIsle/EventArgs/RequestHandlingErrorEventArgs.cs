using System;

namespace HareIsle.EventArgs
{
    /// <summary>
    /// <see cref="RpcHandler{TRequest, TResponse}.RequestHandlingError"/> event data.
    /// </summary>
    /// <typeparam name="TRequest">Request object type.</typeparam>
    public class RequestHandlingErrorEventArgs<TRequest> : System.EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="request">Request object.</param>
        /// <param name="exception">Exception representing an error that occurred while processing the request.</param>
        public RequestHandlingErrorEventArgs(TRequest request, Exception exception)
        {
            Request = request;
            Exception = exception;
        }

        /// <summary>
        /// Request object.
        /// </summary>
        public TRequest Request { get; }

        /// <summary>
        /// Exception representing an error that occurred while processing the request.
        /// </summary>
        public Exception Exception { get; }
    }
}