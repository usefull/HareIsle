namespace HareIsle
{
    /// <summary>
    /// Constants.
    /// </summary>
    internal static class Constant
    {
        /// <summary>
        /// Queue name prefix for RPC requests.
        /// </summary>
        public const string RpcQueueNamePrefix = "rpc_";

        /// <summary>
        /// Prefix of the RabbitMQ exchange name for broadcast messages.
        /// </summary>
        public const string BroadcastExchangeNamePrefix = "broadcast_";

        /// <summary>
        /// List of reserved prefixes.
        /// </summary>
        public static string ReservedPrefixes => $"{RpcQueueNamePrefix}, {BroadcastExchangeNamePrefix}";
    }
}