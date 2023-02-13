using System;

namespace HareIsle.EventArgs
{
    public class InvalidRequestEventArgs : System.EventArgs
    {
        public InvalidRequestEventArgs(ReadOnlyMemory<byte> raw, Exception exception)
        {
            Raw = raw;
            Exception = exception;
        }

        public ReadOnlyMemory<byte> Raw { get; }

        public Exception Exception { get; }
    }
}