using System.ComponentModel.DataAnnotations;

namespace HareIsle.EventArgs
{
    /// <summary>
    /// <see cref="RpcHandler{TRequest, TResponse}.ResponseSent"/> event data.
    /// </summary>
    /// <typeparam name="TRequest">Request object type.</typeparam>
    /// <typeparam name="TResponse">Response object type.</typeparam>
    public class ResponseSentEventArgs<TRequest, TResponse> : System.EventArgs
        where TRequest: class, IValidatableObject
        where TResponse : class, IValidatableObject
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="request">Request object.</param>
        /// <param name="response">Response object.</param>
        public ResponseSentEventArgs(TRequest? request, TResponse response)
        {
            Request = request;
            Response = response;
        }

        /// <summary>
        /// Request object.
        /// </summary>
        public TRequest? Request { get; }

        /// <summary>
        /// Response object.
        /// </summary>
        public TResponse Response { get; }
    }
}