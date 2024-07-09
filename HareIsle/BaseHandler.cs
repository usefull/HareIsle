using HareIsle.Extensions;
using HareIsle.Resources;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

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
            Consumers = new List<EventingBasicConsumer>();
        }

        /// <summary>
        /// RabbitMQ connection.
        /// </summary>
        protected IConnection Connection { get; }

        /// <summary>
        /// RabbitMQ channel.
        /// </summary>
        protected IModel? Channel { get; set; }

        /// <summary>
        /// Actor ID.
        /// </summary>
        protected string ActorId { get; }

        /// <summary>
        /// List of consumers.
        /// </summary>
        protected List<EventingBasicConsumer> Consumers { get; set; }

        /// <summary>
        /// Fires when the handler leaves the active state for some reason.
        /// </summary>
        public event EventHandler<System.EventArgs>? Stopped;

        /// <summary>
        /// Fires when the handler becomes active.
        /// </summary>
        public event EventHandler<System.EventArgs>? Started;

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
        protected EventingBasicConsumer CreateConsumer()
        {
            var consumer = new EventingBasicConsumer(Channel);
            consumer.ConsumerCancelled += OnConsumerCancelled;
            consumer.Registered += OnConsumerRegistered;
            Consumers.Add(consumer);
            return consumer;
        }

        /// <summary>
        /// Consumer successful launch event handler.
        /// </summary>
        /// <param name="sender">Event initiator.</param>
        /// <param name="e">Event arguments.</param>
        private void OnConsumerRegistered(object? sender, ConsumerEventArgs e)
        {
            if (IsRunning)
                Started?.Invoke(this, new System.EventArgs());
        }

        /// <summary>
        /// Consumer canceled event handler.
        /// </summary>
        /// <param name="sender">Event initiator.</param>
        /// <param name="e">Event arguments.</param>
        private void OnConsumerCancelled(object? sender, ConsumerEventArgs e)
        {
            if (!IsRunning)
                Stopped?.Invoke(this, new System.EventArgs());
        }

        /// <summary>
        /// Creates RabbitMQ queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="limit">Maximum queue size. If greater than zero, a queue is created with arguments x-max-length=<paramref name="limit"/> and x-overflow=reject-publish.</param>
        /// <exception cref="ArgumentException">In case of invalid queue name has passed.</exception>
        /// <exception cref="ArgumentNullException">In case of <see cref="Connection"/> property is null.</exception>
        /// <exception cref="AlreadyClosedException">When trying to create a queue on a closed connection.</exception>
        public void DeclareQueue(string queueName, int limit = 0) => DeclareQueue(Connection, queueName, limit);

        /// <summary>
        /// Creates RabbitMQ queue.
        /// </summary>
        /// <param name="connection">RabbitMq connection.</param>
        /// <param name="queueName">Queue name.</param>
        /// <param name="limit">Maximum queue name. If greater than zero, a queue is created with arguments x-max-length=<paramref name="limit"/> and x-overflow=reject-publish.</param
        /// <exception cref="ArgumentNullException">In case of <paramref name="connection"/> parameter is null.</exception>
        /// <exception cref="ArgumentException">In case of invalid queue name has passed.</exception>
        /// <exception cref="AlreadyClosedException">When trying to create a queue on a closed connection.</exception>
        public static void DeclareQueue(IConnection connection, string queueName, int limit = 0)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            queueName.ThrowIfInvalidQueueName();

            using var channel = connection.CreateModel();
            channel.QueueDeclare(
                queueName,
                true,
                false,
                false,
                limit > 0 ? new Dictionary<string, object>() { { "x-max-length", limit }, { "x-overflow", "reject-publish" } } : null
            );
        }

        /// <summary>
        /// Removes the RabbitMQ exchange.
        /// </summary>
        /// <param name="exchangeName">The exchange name to remove.</param>
        /// <exception cref="ArgumentNullException">In case of <see cref="Connection"/> property is null.</exception>
        public void ExchangeDelete(string exchangeName) => ExchangeDelete(Connection, exchangeName);

        /// <summary>
        /// Removes the RabbitMQ exchange.
        /// </summary>
        /// <param name="connection">The RabbitMQ connection.</param>
        /// <param name="exchangeName">The exchange name to remove.</param>
        /// <exception cref="ArgumentNullException">In case of <paramref name="connection"/> is null.</exception>
        public static void ExchangeDelete(IConnection connection, string exchangeName)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            using var channel = connection.CreateModel();
            channel.ExchangeDelete(exchangeName);
        }

        /// <summary>
        /// Returns the number of messages in the queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <returns>The number of essages in the queue.</returns>
        /// <exception cref="ArgumentException">In case of invalid the queue name.</exception>
        /// <exception cref="AlreadyClosedException">When trying to perform the operation on a closed connection.</exception>
        public uint GetMessageCount(string queueName) => GetMessageCount(Connection, queueName);

        /// <summary>
        /// Returns the number of messages in the queue.
        /// </summary>
        /// <param name="connection">The RabbitMQ connection.</param>
        /// <param name="queueName">Queue name.</param>
        /// <returns>The number of essages in the queue.</returns>
        /// <exception cref="ArgumentNullException">In case of <paramref name="connection"/> is null.</exception>
        /// <exception cref="ArgumentException">In case of invalid the queue name.</exception>
        /// <exception cref="AlreadyClosedException">When trying to perform the operation on a closed connection.</exception>
        public static uint GetMessageCount(IConnection connection, string queueName)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            queueName.ThrowIfInvalidQueueName();

            using var channel = connection.CreateModel();
            return channel.QueueDeclarePassive(queueName).MessageCount;
        }

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
                        c.ConsumerCancelled -= OnConsumerCancelled;

                        if (!c.IsRunning)
                            continue;

                        foreach (var t in c.ConsumerTags)
                        {
                            try
                            {
                                Channel.BasicCancel(t);
                            }
                            catch { }
                        }
                    }

                    if (Channel!.IsOpen && !string.IsNullOrWhiteSpace(QueueName))
                        Channel!.QueueDeleteNoWait(QueueName, true, false);

                    try
                    {
                        Channel.Dispose();
                    }
                    catch { }
                }

                Consumers.Clear();

                OnConsumerCancelled(this, new ConsumerEventArgs(new string[] { }));
            }

            disposed = true;
        }

        #endregion
    }
}