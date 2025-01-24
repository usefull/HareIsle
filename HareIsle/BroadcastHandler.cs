using HareIsle.Entities;
using Microsoft.VisualStudio.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace HareIsle
{
    /// <summary>
    /// The broadcast messages handler.
    /// </summary>
    /// <typeparam name="TNotification">The broadcast message type.</typeparam>
    public class BroadcastHandler<TNotification> : BaseHandler
        where TNotification : class, IValidatableObject
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="actorId">An actor ID.</param>
        /// <param name="connection">A RabbitMQ connection.</param>
        /// <param name="broadcastActorId">An ID of the actor-translator of broadcast messages.</param>
        public BroadcastHandler(string actorId, IConnection connection, string broadcastActorId, Action<TNotification> action)
            : base(actorId, connection)
        {
            BroadcastActorId = broadcastActorId;
            Action = action;
            ExchangeName = $"{Constant.BroadcastExchangeNamePrefix}{BroadcastActorId}";

            var jtf = new JoinableTaskFactory(new JoinableTaskContext());

            jtf.Run(async () =>
            {
                Channel = await Connection.CreateChannelAsync();

                await Channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Fanout);
                QueueName = (await Channel.QueueDeclareAsync()).QueueName;
                await Channel.QueueBindAsync(QueueName, ExchangeName, string.Empty);

                var consumer = CreateConsumer();
                consumer.ReceivedAsync += ReceivedAsync;
                await Channel.BasicConsumeAsync(QueueName, true, consumer);
            });
        }

        /// <summary>
        /// The RabbitMQ exchange name through which the broadcast occurs.
        /// </summary>
        public string ExchangeName { get; private set; }

        /// <summary>
        /// The ID of the actor-translator of broadcast messages..
        /// </summary>
        protected string BroadcastActorId { get; set; }

        /// <summary>
        /// The message handle function.
        /// </summary>
        protected Action<TNotification> Action { get; set; }

        /// <summary>
        /// Fires when error occured while message handling.
        /// </summary>
        public event Microsoft.VisualStudio.Threading.AsyncEventHandler<EventArgs.ErrorEventArgs<TNotification, TNotification>>? OnError;

        /// <summary>
        /// Fires when incoming message arrived.
        /// </summary>
        public event Microsoft.VisualStudio.Threading.AsyncEventHandler<EventArgs.IncomingEventArgs<IValidatableObject>>? OnIncoming;

        /// <summary>
        /// Fires when message handling completed.
        /// </summary>
        public event Microsoft.VisualStudio.Threading.AsyncEventHandler<EventArgs.IncomingEventArgs<TNotification>>? OnHandled;

        /// <summary>
        /// Handles the message received event.
        /// </summary>
        /// <param name="sender">The event initiator.</param>
        /// <param name="ea">Event arguments.</param>
        private async Task ReceivedAsync(object? sender, BasicDeliverEventArgs ea)
        {
            byte[]? incomingBytes = null;
            Message<TNotification>? message = null;

            try
            {
                incomingBytes = ea.Body.ToArray();

                try
                {
                    message = Message<TNotification>.FromBytes(incomingBytes);
                }
                catch (Exception ex)
                {
                    if (OnError != null)
                        await OnError.InvokeAsync(this, new EventArgs.ErrorEventArgs<TNotification, TNotification>
                        {
                            ActorId = ActorId,
                            ContrActorId = BroadcastActorId,
                            Exception = ex,
                            Incoming = message?.Payload == null ? default : message.Payload,
                            Outgoing = default,
                            RawIncoming = incomingBytes,
                            Type = EventArgs.ErrorType.Deserializing
                        });

                    return;
                }

                try
                {
                    message.ThrowIfInvalid();
                }
                catch (Exception ex)
                {
                    if (OnError != null)
                        await OnError.InvokeAsync(this, new EventArgs.ErrorEventArgs<TNotification, TNotification>
                        {
                            ActorId = ActorId,
                            ContrActorId = BroadcastActorId,
                            Exception = ex,
                            Incoming = message?.Payload == null ? default : message.Payload,
                            Outgoing = default,
                            RawIncoming = incomingBytes,
                            Type = EventArgs.ErrorType.Validating
                        });

                    return;
                }

                if (OnIncoming != null)
                    await OnIncoming.InvokeAsync(this, new EventArgs.IncomingEventArgs<IValidatableObject>
                    {
                        ActorId = ActorId,
                        ContrActorId = BroadcastActorId,
                        Incoming = message?.Payload
                    });

                Action(message!.Payload!);

                if (OnHandled != null)
                    await OnHandled.InvokeAsync(this, new EventArgs.IncomingEventArgs<TNotification>
                    {
                        ActorId = ActorId,
                        ContrActorId = BroadcastActorId,
                        Incoming = message?.Payload
                    });
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    await OnError.InvokeAsync(this, new EventArgs.ErrorEventArgs<TNotification, TNotification>
                    {
                        ActorId = ActorId,
                        ContrActorId = BroadcastActorId,
                        Exception = ex,
                        Incoming = message?.Payload == null ? default : message.Payload,
                        Outgoing = default,
                        RawIncoming = incomingBytes,
                        Type = EventArgs.ErrorType.Handling
                    });
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
                foreach (var c in Consumers)
                    c.ReceivedAsync -= ReceivedAsync;

                if (!string.IsNullOrWhiteSpace(QueueName))
                    _ = Channel?.QueueDeleteAsync(QueueName);
            }

            disposed = true;
            base.Dispose(disposing);
        }

        #endregion
    }
}