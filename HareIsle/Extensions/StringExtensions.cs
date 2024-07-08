using HareIsle.Resources;
using System;

namespace HareIsle.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="string"/>.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Validates queue name and throws <see cref="ArgumentException"/> if the name is invalid.
        /// </summary>
        /// <param name="queueName">Queue name for validation.</param>
        /// <exception cref="ArgumentException">In case of an invalid queue name or the queue name starts with invalid prefix.</exception>
        public static void ThrowIfInvalidQueueName(this string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
                throw new ArgumentException(Errors.QueueNameCannotBeNullEmptyOrBlank, nameof(queueName));

            if (queueName.StartsWith(Constant.RpcQueueNamePrefix) ||
                queueName.StartsWith(Constant.BroadcastExchangeNamePrefix))
                throw new ArgumentException(string.Format(Errors.InvalidQueueName, Constant.ReservedPrefixes), nameof(queueName));
        }
    }
}