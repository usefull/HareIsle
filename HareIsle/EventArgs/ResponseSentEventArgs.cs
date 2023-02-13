using System.ComponentModel.DataAnnotations;

namespace HareIsle.EventArgs
{
    public class ResponseSentEventArgs<TRequest, TResponse> : System.EventArgs
        where TRequest: class, IValidatableObject
        where TResponse : class, IValidatableObject
    {
        public ResponseSentEventArgs(TRequest? request, TResponse response)
        {
            Request = request;
            Response = response;
        }

        public TRequest? Request { get; }

        public TResponse Response { get; }
    }
}