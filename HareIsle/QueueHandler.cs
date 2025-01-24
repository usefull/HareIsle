using HareIsle.Entities;
using HareIsle.Exceptions;
using HareIsle.Extensions;
using HareIsle.Resources;
using Microsoft.VisualStudio.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace HareIsle
{
    /// <summary>
    /// RabbitMQ queue handler.
    /// </summary>
    /// <typeparam name="TPayload">Message type.</typeparam>
    public class QueueHandler<TPayload> : BaseHandler
        where TPayload : class, IValidatableObject
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="actorId">Actor ID.</param>
        /// <param name="queueName">RabbitMQ queue name.</param>
        /// <param name="connection">RabbitMQ connection.</param>
        /// <param name="concurrency">Concurrency (number of consumers processing incoming messages in parallel).</param>
        /// <param name="func">Message handler function.</param>
        /// <exception cref="SubscriptionException">In case of subscription error.</exception>
        /// <exception cref="ArgumentNullException">In case of null has passed to <paramref name="connection"/> or <paramref name="func"/> params.</exception>
        /// <exception cref="ArgumentException">In case of invalid params.</exception>
        /// <exception cref="ArgumentOutOfRangeException">In case of <paramref name="concurrency"/> param is out of range.</exception>
        public QueueHandler(string actorId, IConnection connection, string queueName, int concurrency, Action<TPayload> func)
            : base(actorId, connection)
        {
            queueName.ThrowIfInvalidQueueName();

            if (concurrency < 1 || concurrency > 10)
                throw new ArgumentOutOfRangeException(nameof(concurrency), concurrency, Errors.ConcurrentCustomersOutOfRange);

            Action = func ?? throw new ArgumentNullException(nameof(func));
            QueueName = queueName;

            Exception? ex = null;
            var jtf = new JoinableTaskFactory(new JoinableTaskContext());
            jtf.Run(async () =>
            {
                try
                {
                    Channel = await Connection.CreateChannelAsync();
                    await Channel.BasicQosAsync(0, 1, false);

                    for (int i = 0; i < concurrency; i++)
                    {
                        var consumer = CreateConsumer();
                        consumer.ReceivedAsync += OnReceivedAsync;
                        await Channel.BasicConsumeAsync(QueueName, false, consumer);
                    }
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });

            
            if (ex != null)
            {
                throw new SubscriptionException(string.Format(Errors.SubscriptionError, queueName), ex);
            }
        }

        /// <summary>
        /// Handles an event of incoming message.
        /// </summary>
        /// <param name="sender">The event initiator.</param>
        /// <param name="ea">Event arguments.</param>
        private async Task OnReceivedAsync(object? sender, BasicDeliverEventArgs ea)
        {
            Message<TPayload>? message = null;
            byte[]? incomingBytes = null;

            try
            {
                incomingBytes = ea.Body.ToArray();

                try
                {
                    message = Message<TPayload>.FromBytes(incomingBytes);

                    if (OnIncoming != null)
                        await OnIncoming.InvokeAsync(this, new EventArgs.IncomingEventArgs<TPayload>
                        {
                            ActorId = ActorId,
                            Incoming = message.Payload
                        });
                }
                catch (Exception e)
                {
                    if (OnError  != null)
                        await OnError.InvokeAsync(this, new EventArgs.ErrorEventArgs<TPayload, TPayload>
                        {
                            ActorId = ActorId,
                            Exception = e,
                            RawIncoming = incomingBytes,
                            Type = EventArgs.ErrorType.Deserializing
                        });
                    await Channel!.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    return;
                }

                try
                {
                    message.ThrowIfInvalid();
                }
                catch (Exception e)
                {
                    if (OnError != null)
                        await OnError.InvokeAsync(this, new EventArgs.ErrorEventArgs<TPayload, TPayload>
                        {
                            ActorId = ActorId,
                            Exception = e,
                            Incoming = message.Payload,
                            Type = EventArgs.ErrorType.Validating
                        });
                    await Channel!.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    return;
                }

                try
                {
                    Action(message.Payload!);

                    if (OnHandled != null)
                        await OnHandled.InvokeAsync(this, new EventArgs.IncomingEventArgs<TPayload>
                        {
                            ActorId = ActorId,
                            Incoming = message.Payload
                        });
                }
                catch (Exception e)
                {
                    if (OnError != null)
                        await OnError.InvokeAsync(this, new EventArgs.ErrorEventArgs<TPayload, TPayload>
                        {
                            ActorId = ActorId,
                            Exception = e,
                            Incoming = message.Payload,
                            Type = EventArgs.ErrorType.Handling
                        });
                }
                finally
                {
                    await Channel!.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            }
            catch (Exception e)
            {
                if (OnError != null)
                    await OnError.InvokeAsync(this, new EventArgs.ErrorEventArgs<TPayload, TPayload>
                    {
                        ActorId = ActorId,
                        Exception = e,
                        Incoming = message.Payload == null ? default : message.Payload,
                        Outgoing = message.Payload,
                        RawIncoming = incomingBytes,
                        Type = EventArgs.ErrorType.Acking
                    });
            }
        }

        /// <summary>
        /// Fires when an error occurs while handling a message.
        /// </summary>
        public event Microsoft.VisualStudio.Threading.AsyncEventHandler<EventArgs.ErrorEventArgs<TPayload, TPayload>>? OnError;

        /// <summary>
        /// Fires when an incoming message arrived.
        /// </summary>
        public event Microsoft.VisualStudio.Threading.AsyncEventHandler<EventArgs.IncomingEventArgs<TPayload>>? OnIncoming;

        /// <summary>
        /// Fires when a message completly handled.
        /// </summary>
        public event Microsoft.VisualStudio.Threading.AsyncEventHandler<EventArgs.IncomingEventArgs<TPayload>>? OnHandled;

        /// <summary>
        /// Функция обработки входящих сообщений.
        /// </summary>
        private readonly Action<TPayload> Action;

        #region IDisposable interface implementation

        bool disposed = false;

        /// <summary>
        /// Terminates the handler.
        /// </summary>
        /// <param name="disposing">The indication that the termination is in progress.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                foreach (var c in Consumers)
                    c.ReceivedAsync -= OnReceivedAsync;

                QueueName = null;
            }

            disposed = true;
            base.Dispose(disposing);
        }

        #endregion
    }
}