using System;
using System.Text.RegularExpressions;

namespace HareIsle.Extensions
{
    internal static class StringExtensions
    {
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