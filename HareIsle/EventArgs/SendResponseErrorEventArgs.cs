using System;
using System.ComponentModel.DataAnnotations;

namespace HareIsle.EventArgs
{
    /// <summary>
    /// <see cref="RpcHandler{TRequest, TResponse}.SendResponseError"/> event data.
    /// </summary>
    /// <typeparam name="TResponse">Response object type.</typeparam>
    public class SendResponseErrorEventArgs<TResponse> : System.EventArgs
        where TResponse : class, IValidatableObject
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="response">Response object.</param>
        /// <param name="exception">Exception thrown while sending response.</param>
        public SendResponseErrorEventArgs(TResponse response, Exception exception)
        {
            Response = response;
            Exception = exception;
        }

        /// <summary>
        /// Response object.
        /// </summary>
        public TResponse Response { get; }

        /// <summary>
        /// Exception thrown while sending response.
        /// </summary>
        public Exception Exception { get; }
    }
}