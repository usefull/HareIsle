using HareIsle.Entities;
using HareIsle.Exceptions;
using HareIsle.Extensions;
using HareIsle.Resources;
using RabbitMQ.Client;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Threading;

namespace HareIsle
{
    /// <summary>
    /// Message sending functionality
    /// </summary>
    public class Emitter : BaseHandler
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="actorId">An actor ID.</param>
        /// <param name="connection">A RabbitMQ connection.</param>
        public Emitter(string actorId, IConnection connection) : base(actorId, connection) { }

        /// <summary>
        /// Publishes broadcast message.
        /// </summary>
        /// <typeparam name="TNotification">A broadcast message type.</typeparam>
        /// <param name="notification">A broadcast message.</param>
        /// <exception cref="SerializationException">В случае ошибки сериализации сообщения перед отправкой.</exception>
        /// <exception cref="SendingException">В случае ошибки отправки сообщения.</exception>
        public void Broadcast<TNotification>(TNotification notification)
            where TNotification : class, IValidatableObject
        {
            var exchange = $"{Constant.BroadcastExchangeNamePrefix}{ActorId}";
            var message = new Message<TNotification>
            {
                Payload = notification
            };
            var body = message.ToBytes();

            try
            {
                using var channel = Connection.CreateModel();
                channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout);
                channel.BasicPublish(exchange, string.Empty, null, body);
            }
            catch (Exception ex)
            {
                throw new SendingException(Errors.SendingError, ex);
            }
        }

        /// <summary>
        /// Sends a message to a queue.
        /// </summary>
        /// <typeparam name="TPayload">The message type.</typeparam>
        /// <param name="queueName">The RabbitMQ queue name.</param>
        /// <param name="payload">The message.</param>
        /// <exception cref="SendingException">In case of a message sending error.</exception>
        /// <exception cref="SerializationException">In case of a message serializing error.</exception>
        /// <exception cref="ArgumentException">In case of queue name ia invalid.</exception>
        public void Enqueue<TPayload>(string queueName, TPayload payload)
            where TPayload : class, IValidatableObject
        {
            queueName.ThrowIfInvalidQueueName();

            var message = new Message<TPayload>
            {
                Payload = payload
            };
            var body = message.ToBytes();

            try
            {
                using var channel = Connection.CreateModel();
                channel.ConfirmSelect();

                var eventAck = new AutoResetEvent(false);
                var eventNack = new AutoResetEvent(false);
                var eventReturned = new AutoResetEvent(false);

                channel.BasicAcks += (s, ea) => eventAck.Set();
                channel.BasicNacks += (s, ea) => eventNack.Set();
                channel.BasicReturn += (s, ea) => eventReturned.Set();

                channel.BasicPublish(string.Empty, queueName, true, null, body);
                int i = WaitHandle.WaitAny(new WaitHandle[] { eventAck, eventReturned, eventNack }, TimeSpan.FromSeconds(5));
                if (i == 1)
                    throw new MessageRoutingException();
                else if (i == 2)
                    throw new MessageNackException();
                else if (i > 2)
                    throw new SendingException(Errors.CouldNotWaitForAck);

            }
            catch (Exception ex)
            {
                throw new SendingException(Errors.SendingError, ex);
            }
        }

        #region IDisposable interface implementation

        bool disposed = false;

        /// <summary>
        /// Terminates the handler.
        /// </summary>
        /// <param name="disposing">An indication that termination is in progress.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Call Dispose for managed objects created by the handler.
                QueueName = null;
            }

            disposed = true;
            base.Dispose(disposing);
        }

        #endregion
    }
}