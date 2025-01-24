using HareIsle.Entities;
using HareIsle.Extensions;
using HareIsle.Resources;
using Microsoft.VisualStudio.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HareIsle
{
    /// <summary>
    /// The base class of the handler for the elementary interaction function.
    /// </summary>
    public abstract class BaseHandler : IDisposable
    {
        /// <summary>
        /// Protected constructor.
        /// </summary>
        /// <param name="actorId">Actor ID.</param>
        /// <param name="connection">RabbitMQ connection.</param>
        /// <exception cref="ArgumentNullException">In case of null RabbitMQ connection has passed.</exception>
        /// <exception cref="ArgumentException">In case of closed RabbitMQ connection has passed or Actor ID is null, empty or blank.</exception>
        protected BaseHandler(string actorId, IConnection connection)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (!connection.IsOpen)
                throw new ArgumentException(Errors.HandlerCreationOnClosedConnection, nameof(connection));

            if (string.IsNullOrWhiteSpace(actorId))
                throw new ArgumentException(Errors.ActorIdCannotBeNullEmptyOrBlank, nameof(actorId));

            Connection = connection;
            ActorId = actorId;
            Consumers = new List<AsyncEventingBasicConsumer>();
        }

        /// <summary>
        /// RabbitMQ connection.
        /// </summary>
        protected IConnection Connection { get; }

        /// <summary>
        /// RabbitMQ channel.
        /// </summary>
        protected IChannel? Channel { get; set; }

        /// <summary>
        /// Actor ID.
        /// </summary>
        protected string ActorId { get; }

        /// <summary>
        /// List of consumers.
        /// </summary>
        protected List<AsyncEventingBasicConsumer> Consumers { get; set; }

        /// <summary>
        /// Fires when the handler leaves the active state for some reason.
        /// </summary>
        public event Microsoft.VisualStudio.Threading.AsyncEventHandler<System.EventArgs>? Stopped;

        /// <summary>
        /// Fires when the handler becomes active.
        /// </summary>
        public event Microsoft.VisualStudio.Threading.AsyncEventHandler<System.EventArgs>? Started;

        /// <summary>
        /// The RabbitMQ queue name the handler is working with.
        /// </summary>
        public string? QueueName { get; protected set; }

        /// <summary>
        /// Sign that the handler is active,
        /// i.e. has at least one consumer waiting for incoming messages.
        /// </summary>
        public bool IsRunning => Consumers != null && Consumers.Any(c => c.IsRunning);

        /// <summary>
        /// Creates a RabbitMq consumer.
        /// </summary>
        /// <returns>RabbitMq consumer.</returns>
        /// <remarks>Use this method in inherited classes to create consumers,
        /// if you want consumers to generate a <seealso cref="Stopped"/> event and affect the property <seealso cref="IsRunning"/>.</remarks>
        protected AsyncEventingBasicConsumer CreateConsumer()
        {
            var consumer = new AsyncEventingBasicConsumer(Channel);
            consumer.UnregisteredAsync += OnConsumerUnregisteredAsync;
            consumer.RegisteredAsync += OnConsumerRegisteredAsync;
            Consumers.Add(consumer);
            return consumer;
        }

        /// <summary>
        /// Consumer canceled event handler.
        /// </summary>
        /// <param name="sender">The event initiator.</param>
        /// <param name="args">The event arguments.</param>
        private async Task OnConsumerUnregisteredAsync(object sender, ConsumerEventArgs args)
        {
            if (!IsRunning && Stopped != null)
                await Stopped.InvokeAsync(this, new System.EventArgs());
        }

        /// <summary>
        /// Consumer successful launch event handler.
        /// </summary>
        /// <param name="sender">Event initiator.</param>
        /// <param name="e">Event arguments.</param>
        private async Task OnConsumerRegisteredAsync(object? sender, ConsumerEventArgs e)
        {
            if (IsRunning && Started != null)
                await Started.InvokeAsync(this, new System.EventArgs());
        }

        /// <summary>
        /// Creates RabbitMQ queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="limit">Maximum queue size. If greater than zero, a queue is created with arguments x-max-length=<paramref name="limit"/> and x-overflow=reject-publish.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task to await to wait for declare to complete.</returns>
        /// <exception cref="ArgumentException">In case of invalid queue name has passed.</exception>
        /// <exception cref="ArgumentNullException">In case of <see cref="Connection"/> property is null.</exception>
        /// <exception cref="AlreadyClosedException">When trying to create a queue on a closed connection.</exception>
        public async Task DeclareQueueAsync(string queueName, int limit = 0, CancellationToken cancellationToken = default) =>
            await DeclareQueueAsync(Connection, queueName, limit, cancellationToken);

        /// <summary>
        /// Creates RabbitMQ queue.
        /// </summary>
        /// <param name="connection">RabbitMq connection.</param>
        /// <param name="queueName">Queue name.</param>
        /// <param name="limit">Maximum queue name. If greater than zero, a queue is created with arguments x-max-length=<paramref name="limit"/> and x-overflow=reject-publish.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task to await to wait for declare to complete.</returns>
        /// <exception cref="ArgumentNullException">In case of <paramref name="connection"/> parameter is null.</exception>
        /// <exception cref="ArgumentException">In case of invalid queue name has passed.</exception>
        /// <exception cref="AlreadyClosedException">When trying to create a queue on a closed connection.</exception>
        public static async Task DeclareQueueAsync(IConnection connection, string queueName, int limit = 0, CancellationToken cancellationToken = default)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            queueName.ThrowIfInvalidQueueName();

            using var channel = await connection.CreateChannelAsync(null, cancellationToken);
            await channel.QueueDeclareAsync(
                queueName,
                true,
                false,
                false,
                limit > 0 ? new Dictionary<string, object?>() { { "x-max-length", limit }, { "x-overflow", "reject-publish" } } : null,
                false,
                cancellationToken
            );
        }

        /// <summary>
        /// Removes the RabbitMQ exchange.
        /// </summary>
        /// <param name="exchangeName">The exchange name to remove.</param>
        /// <returns>The task to await to wait for delete to complete.</returns>
        /// <exception cref="ArgumentNullException">In case of <see cref="Connection"/> property is null.</exception>
        public async Task ExchangeDeleteAsync(string exchangeName, CancellationToken cancellationToken = default) =>
            await ExchangeDeleteAsync(Connection, exchangeName, cancellationToken);

        /// <summary>
        /// Removes the RabbitMQ exchange.
        /// </summary>
        /// <param name="connection">The RabbitMQ connection.</param>
        /// <param name="exchangeName">The exchange name to remove.</param>
        /// <returns>The task to await to wait for delete to complete.</returns>
        /// <exception cref="ArgumentNullException">In case of <paramref name="connection"/> is null.</exception>
        public static async Task ExchangeDeleteAsync(IConnection connection, string exchangeName, CancellationToken cancellationToken = default)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            using var channel = await connection.CreateChannelAsync(null, cancellationToken);
            await channel.ExchangeDeleteAsync(exchangeName, false, false, cancellationToken);
        }

        /// <summary>
        /// Returns the number of messages in the queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <returns>The number of essages in the queue.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous count operation. The value of its Result property contains the message count in the queue.</returns>
        /// <exception cref="ArgumentException">In case of invalid the queue name.</exception>
        /// <exception cref="AlreadyClosedException">When trying to perform the operation on a closed connection.</exception>
        public async Task<uint> GetMessageCountAsync(string queueName, CancellationToken cancellationToken = default) =>
            await GetMessageCountAsync(Connection, queueName, cancellationToken);

        /// <summary>
        /// Returns the number of messages in the queue.
        /// </summary>
        /// <param name="connection">The RabbitMQ connection.</param>
        /// <param name="queueName">The queue name.</param>
        /// <returns>The number of essages in the queue.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous count operation. The value of its Result property contains the message count in the queue.</returns>
        /// <exception cref="ArgumentNullException">In case of <paramref name="connection"/> is null.</exception>
        /// <exception cref="ArgumentException">In case of invalid the queue name.</exception>
        /// <exception cref="AlreadyClosedException">When trying to perform the operation on a closed connection.</exception>
        public static async Task<uint> GetMessageCountAsync(IConnection connection, string queueName, CancellationToken cancellationToken = default)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            queueName.ThrowIfInvalidQueueName();

            using var channel = await connection.CreateChannelAsync(null, cancellationToken);
            return (await channel.QueueDeclarePassiveAsync(queueName, cancellationToken)).MessageCount;
        }

        /// <summary>
        /// Extracts the next message from the queue.
        /// </summary>
        /// <typeparam name="TPayload">The message type.</typeparam>
        /// <param name="connection">The RabbitMQ connection.</param>
        /// <param name="queueName">The queue name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of its Result property contains the extracted message.</returns>
        /// <exception cref="ArgumentNullException">In case of the<paramref name="connection"/> is null.</exception>
        /// <exception cref="ArgumentException">In case of the <paramref name="queueName"/> is null, empty, blank or invalid.</exception>
        /// <exception cref="ValidationException">In case of the extracted message is invalid.</exception>
        /// <exception cref="System.Runtime.Serialization.SerializationException">In case of the extracted message deserializing error.</exception>
        public static async Task<Message<TPayload>?> GetMessageAsync<TPayload>(IConnection connection, string queueName, CancellationToken cancellationToken = default)
            where TPayload : class, IValidatableObject
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            queueName.ThrowIfInvalidQueueName();

            using var channel = await connection.CreateChannelAsync(null, cancellationToken);

            var getResult = await channel.BasicGetAsync(queueName, true, cancellationToken);
            if (getResult == null)
                return null;

            var message = Message<TPayload>.FromBytes(getResult.Body.ToArray());
            message?.ThrowIfInvalid();

            return message;
        }

        /// <summary>
        /// Extracts the next message from the queue.
        /// </summary>
        /// <typeparam name="TPayload">The message type.</typeparam>
        /// <param name="queueName">The queue name.</param>
        /// <returns>An extracted message.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of its Result property contains the extracted message.</returns>
        /// <exception cref="ArgumentException">In case of the <paramref name="queueName"/> is null, empty, blank or invalid.</exception>
        /// <exception cref="ValidationException">In case of the extracted message is invalid.</exception>
        /// <exception cref="System.Runtime.Serialization.SerializationException">In case of the extracted message deserializing error.</exception>
        public async Task<Message<TPayload>?> GetMessageAsync<TPayload>(string queueName, CancellationToken cancellationToken = default)
            where TPayload : class, IValidatableObject => await GetMessageAsync<TPayload>(Connection, queueName, cancellationToken);

        #region IDisposable interface implementation

        bool disposed = false;

        /// <summary>
        /// Terminates the handler.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Terminates the handler.
        /// </summary>
        /// <param name="disposing">Indisates that termination is in progress.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (Channel != null)
                {
                    foreach (var c in Consumers)
                    {
                        c.UnregisteredAsync -= OnConsumerUnregisteredAsync;

                        if (!c.IsRunning)
                            continue;

                        foreach (var t in c.ConsumerTags)
                        {
                            try
                            {
                                _ = Channel.BasicCancelAsync(t, true);
                            }
                            catch { }
                        }
                    }

                    if (Channel!.IsOpen && !string.IsNullOrWhiteSpace(QueueName))
                        _ = Channel!.QueueDeleteAsync(QueueName, true, false, true);

                    try
                    {
                        Channel.Dispose();
                    }
                    catch { }
                }

                Consumers.Clear();

                _ = OnConsumerUnregisteredAsync(this, new ConsumerEventArgs(new string[] { }));
            }

            disposed = true;
        }

        #endregion
    }
}