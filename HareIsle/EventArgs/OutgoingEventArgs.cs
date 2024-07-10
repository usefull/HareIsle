using System.ComponentModel.DataAnnotations;

namespace HareIsle.EventArgs
{
    /// <summary>
    /// Fires when an outgoing message has sent.
    /// </summary>
    /// <typeparam name="TIncoming">The type of the object representing the incoming message in response to which the outgoing message was sent.</typeparam>
    /// <typeparam name="TOutgoing">The outgoing message type.</typeparam>
    public class OutgoingEventArgs<TIncoming, TOutgoing> : System.EventArgs
        where TIncoming : class, IValidatableObject
        where TOutgoing : class, IValidatableObject
    {
        /// <summary>
        /// The actor ID.
        /// </summary>
        public string? ActorId { get; set; }

        /// <summary>
        /// The incoming message in response to which the outgoing message was sent.
        /// </summary>
        public TIncoming? Incoming { get; set; }

        /// <summary>
        /// The outgoing message.
        /// </summary>
        public TOutgoing? Outgoing { get; set; }
    }
}