using System;

namespace HareIsle.Entities
{
    /// <summary>
    /// Undeserialized RPC response.
    /// </summary>
    internal class UndeserializedRpcResponse
    {
        /// <summary>
        /// RPC response as raw string.
        /// </summary>
        public string? Response { get; set; }

        /// <summary>
        /// Exception represents a response message UTF-8 decoding error, if any.
        /// </summary>
        public Exception? Exception { get; set; }
    }
}