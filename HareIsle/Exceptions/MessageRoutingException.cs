using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    /// <summary>
    /// Throws in case of an attempt is made to send a message to a non-existent queue.
    /// </summary>
    //public class MessageRoutingException : Exception
    //{
    //    /// <summary>
    //    /// Default constructor.
    //    /// </summary>
    //    public MessageRoutingException() : base(Errors.MessageRoutingError) { }

    //    /// <summary>
    //    /// Constructor.
    //    /// </summary>
    //    /// <param name="message">Error message.</param>
    //    public MessageRoutingException(string? message) : base(message) { }

    //    /// <summary>
    //    /// Constructor.
    //    /// </summary>
    //    /// <param name="message">Error message.</param>
    //    /// <param name="innerException">The exception that caused the error.</param>
    //    public MessageRoutingException(string? message, Exception? innerException) : base(message, innerException) { }

    //    /// <summary>
    //    /// Constructor.
    //    /// </summary>
    //    /// <param name="info">Serialization params.</param>
    //    /// <param name="context">Serialization context.</param>
    //    protected MessageRoutingException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    //}
}