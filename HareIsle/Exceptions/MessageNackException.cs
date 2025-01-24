using HareIsle.Resources;
using System;
using System.Runtime.Serialization;

namespace HareIsle.Exceptions
{
    /// <summary>
    /// Throws in case of a lack of confirmation of sending a message to the queue.
    /// </summary>
    //public class MessageNackException : Exception
    //{
    //    /// <summary>
    //    /// Default constructor.
    //    /// </summary>
    //    public MessageNackException() : base(Errors.MessageNackError) { }

    //    /// <summary>
    //    /// Constructor.
    //    /// </summary>
    //    /// <param name="message">Error message.</param>
    //    public MessageNackException(string? message) : base(message) { }

    //    /// <summary>
    //    /// Constructor.
    //    /// </summary>
    //    /// <param name="message">Error message.</param>
    //    /// <param name="innerException">The exception that caused the error.</param>
    //    public MessageNackException(string? message, Exception? innerException) : base(message, innerException) { }

    //    /// <summary>
    //    /// Constructor.
    //    /// </summary>
    //    /// <param name="info">Serialization params.</param>
    //    /// <param name="context">Serialization context.</param>
    //    protected MessageNackException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    //}
}