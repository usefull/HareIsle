using System;

namespace HareIsle.EventArgs
{
    /// <summary>
    /// <see cref="RpcHandler{TRequest, TResponse}.InvalidRequest"/> event data.
    /// </summary>
    public class InvalidRequestEventArgs : System.EventArgs
    {
        /// <summary>
        /// Costructor.
        /// </summary>
        /// <param name="raw">Raw request message body.</param>
        /// <param name="exception">Exception describing a request validation problem.</param>
        public InvalidRequestEventArgs(ReadOnlyMemory<byte> raw, Exception exception)
        {
            Raw = raw;
            Exception = exception;
        }

        /// <summary>
        /// Raw request message body.
        /// </summary>
        public ReadOnlyMemory<byte> Raw { get; }

        /// <summary>
        /// Exception describing a request validation problem.
        /// </summary>
        public Exception Exception { get; }
    }
}