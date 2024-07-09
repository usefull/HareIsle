using System;
using System.ComponentModel.DataAnnotations;

namespace HareIsle.EventArgs
{
    /// <summary>
    /// Error event arguments.
    /// </summary>
    /// <typeparam name="TIncoming">Incoming message type.</typeparam>
    /// <typeparam name="TOutgoing">Outgoing message type.</typeparam>
    public class ErrorEventArgs<TIncoming, TOutgoing> : System.EventArgs
        where TIncoming : class, IValidatableObject
        where TOutgoing : class, IValidatableObject
    {
        /// <summary>
        /// An actor ID.
        /// </summary>
        public string? ActorId { get; set; }

        /// <summary>
        /// The contr-actor ID.
        /// </summary>
        public string? ContrActorId { get; set; }

        /// <summary>
        /// Error type.
        /// </summary>
        public ErrorType Type { get; set; }

        /// <summary>
        /// The exception that caused the error.
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Raw incoming message.
        /// </summary>
        public byte[]? RawIncoming { get; set; }

        /// <summary>
        /// Incoming message.
        /// </summary>
        public TIncoming? Incoming { get; set; }

        /// <summary>
        /// Outgoing message.
        /// </summary>
        public TOutgoing? Outgoing { get; set; }
    }

    /// <summary>
    /// Error types.
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// Unknown error.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Incoming message deserializing error.
        /// </summary>
        Deserializing,

        /// <summary>
        /// Incoming message validation error.
        /// </summary>
        Validating,

        /// <summary>
        /// Message handling error.
        /// </summary>
        Handling,

        /// <summary>
        /// Outgoing message serialization error.
        /// </summary>
        Serializing,

        /// <summary>
        /// Outgoing message sending error.
        /// </summary>
        Sending,

        /// <summary>
        /// Message acking error.
        /// </summary>
        Acking
    }
}