using System;
using System.Text.RegularExpressions;

namespace HareIsle.Extensions
{
    /// <summary>
    /// Extensions methods for <see cref="string"/>.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Checks if a string is a valid custom queue name.
        /// </summary>
        /// <param name="queueName">Queue name to check.</param>
        /// <returns>True if string is valid queue name, otherwise - false.</returns>
        public static bool IsValidQueueName(this string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
                return false;

            if (queueName.StartsWith("amq.", StringComparison.OrdinalIgnoreCase))
                return false;

            if (queueName.Length > 255)
                return false;

            return regexQueueName.IsMatch(queueName);
        }

        private static readonly Regex regexQueueName = new Regex(@"^[\x20-\x7e]+$");
    }
}