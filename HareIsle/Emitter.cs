using HareIsle.Entities;
using HareIsle.Exceptions;
using HareIsle.Extensions;
using HareIsle.Resources;
using Microsoft.VisualStudio.Threading;
using RabbitMQ.Client;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace HareIsle
{
    /// <summary>
    /// Message sending functionality
    /// </summary>
    public class Emitter : BaseHandler
    {
        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="actorId">The actor ID.</param>
        /// <param name="connection">The RabbitMQ connection.</param>
        public Emitter(string actorId, IConnection connection) : base(actorId, connection) { }

        /// <summary>
        /// Publishes a broadcast message.
        /// </summary>
        /// <typeparam name="TNotification">A broadcast message type.</typeparam>
        /// <param name="notification">The broadcast message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task to await to wait for publishing to complete.</returns>
        /// <exception cref="SerializationException">In case of a serialization error.</exception>
        /// <exception cref="SendingException">In case of a sending error.</exception>
        public async Task BroadcastAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
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
                using var channel = await Connection.CreateChannelAsync(null, cancellationToken);
                await channel.ExchangeDeclareAsync(exchange: exchange, type: ExchangeType.Fanout, cancellationToken: cancellationToken);
                await channel.BasicPublishAsync(exchange, string.Empty, body, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new SendingException(Errors.SendingError, ex);
            }
        }

        /// <summary>
        /// Sends a message to a queue.
        /// </summary>
        /// <typeparam name="TPayload">A message type.</typeparam>
        /// <param name="queueName">The RabbitMQ queue name.</param>
        /// <param name="payload">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task to await to wait for sending to complete.</returns>
        /// <exception cref="SendingException">In case of a message sending error.</exception>
        /// <exception cref="SerializationException">In case of a message serializing error.</exception>
        /// <exception cref="ArgumentException">In case of queue name ia invalid.</exception>
        public async Task EnqueueAsync<TPayload>(string queueName, TPayload payload, CancellationToken cancellationToken = default)
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
                var channelOpts = new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true
                );
                using var channel = await Connection.CreateChannelAsync(channelOpts, cancellationToken);

                //var eventAck = new AutoResetEvent(false);
                //var eventNack = new AutoResetEvent(false);
                //var eventReturned = new AutoResetEvent(false);

                //channel.BasicAcksAsync += async (s, ea) =>
                //{
                //    await Task.Run(() => eventAck.Set());
                //};
                //channel.BasicNacksAsync += async (s, ea) =>
                //{
                //    await Task.Run(() => eventNack.Set());
                //};
                //channel.BasicReturnAsync += async (s, ea) =>
                //{
                //    await Task.Run(() => eventReturned.Set());
                //};

                await channel.BasicPublishAsync(string.Empty, queueName, true, body, cancellationToken);

                //var taskAck = eventAck.ToTask();
                //var taskReturned = eventReturned.ToTask();
                //var taskNack = eventNack.ToTask();
                //var taskExpired = Task.Delay(5000);

                //var t = await Task.WhenAny(taskAck, taskReturned, taskNack, taskExpired);
                //if (ReferenceEquals(t, taskReturned))
                //    throw new MessageRoutingException();
                //else if (ReferenceEquals(t, taskNack))
                //    throw new MessageNackException();
                //else if (ReferenceEquals(t, taskExpired))
                //    throw new SendingException(Errors.CouldNotWaitForAck);
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
                QueueName = null;

            disposed = true;
            base.Dispose(disposing);
        }

        #endregion
    }
}