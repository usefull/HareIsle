using System.ComponentModel.DataAnnotations;

namespace HareIsle.EventArgs
{
    /// <summary>
    /// New incoming message event arguments.
    /// </summary>
    /// <typeparam name="TIncoming">Incoming message type.</typeparam>
    public class IncomingEventArgs<TIncoming> : System.EventArgs
        where TIncoming : class, IValidatableObject
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
        /// Incoming message.
        /// </summary>
        public TIncoming? Incoming { get; set; }
    }
}