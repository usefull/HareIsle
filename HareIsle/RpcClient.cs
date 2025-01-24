using HareIsle.Entities;
using HareIsle.Exceptions;
using HareIsle.Resources;
using Microsoft.VisualStudio.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace HareIsle
{
    /// <summary>
    /// The RPC-query client.
    /// </summary>
    public class RpcClient : BaseHandler
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="actorId">The actor ID.</param>
        /// <param name="connection">The RabbitMQ connection.</param>
        /// <exception cref="ArgumentNullException">In case of null has passed into <paramref name="connection"/> param.</exception>
        /// <exception cref="ArgumentException">In case of <paramref name="actorId"/> param is null, empty or blank.</exception>
        public RpcClient(string actorId, IConnection connection)
            : base(actorId, connection)
        {
            // The default request response time.
            Timeout = 15;
        }

        /// <summary>
        /// Executes a RPC-query.
        /// </summary>
        /// <typeparam name="TRequest">The RPC-query object type.</typeparam>
        /// <typeparam name="TResponse">The RPC response object type.</typeparam>
        /// <param name="requestedActorId">The request recipient actor ID.</param>
        /// <param name="request">The RPC query object.</param>
        /// <param name="timeout">The request timeout in seconds after which an <see cref="TimeoutException"/> occurs.
        /// If the value is less than or equal to 0, the timeout value is taken from the <see cref="Timeout"/> property.</param>
        /// <param name="cancellationToken">The operation cancellation token.</param>
        /// <returns>The task that represents the asynchronous request execution operation.</returns>
        /// <exception cref="TimeoutException">In case of time has out.</exception>
        /// <exception cref="SendingException">In case of an error while request sending.</exception>
        /// <exception cref="AlreadyClosedException">In case the RabbitMQ connection is lost while waiting for a response or trying to execute a request on an already closed connection.</exception>
        /// <exception cref="OperationCanceledException">In case of RPC query cancellation forcibly by <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="RpcHandlingException">In case of a RPC query handling error.</exception>
        /// <exception cref="SerializationException">In case of a response deserializing error.</exception>
        /// <exception cref="ValidationException">In case of a response validation error.</exception>
        public async Task<TResponse> CallAsync<TRequest, TResponse>(string requestedActorId, TRequest request, int timeout = 0, CancellationToken cancellationToken = default)
            where TRequest : class, IValidatableObject
            where TResponse : class, IValidatableObject
        {
            var responseEvent = new AsyncAutoResetEvent();
            TResponse? response = default;
            Exception? exception = null;
            AsyncEventingBasicConsumer? consumer = null;
            IChannel? channel = null;

            var messageTTL = (timeout == 0 ? Timeout : timeout) * 1000;

            #region The query sending

            try
            {
                channel = await Connection.CreateChannelAsync(null, cancellationToken);

                var replyQueueName = (await channel.QueueDeclareAsync(cancellationToken: cancellationToken)).QueueName;
                var correlationId = Guid.NewGuid().ToString();

                consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (model, ea) => await Task.Run(() =>
                {
                    try
                    {
                        if (ea.BasicProperties.CorrelationId != correlationId)
                            return;

                        var body = ea.Body.ToArray();

                        RpcHandlingError? rpcHandlingError = null;
                        try
                        {
                            var resp = Message<RpcHandlingError>.FromBytes(body);
                            resp.ThrowIfInvalid();
                            rpcHandlingError = resp.Payload;
                        }
                        catch { }

                        if (rpcHandlingError != null)
                            throw new RpcHandlingException(rpcHandlingError.ErrorMessage);

                        var deserialized = Message<TResponse>.FromBytes(body);
                        deserialized.ThrowIfInvalid();
                        response = deserialized.Payload;
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                    finally
                    {
                        responseEvent.Set();
                    }
                });
                await channel.BasicConsumeAsync(replyQueueName, true, consumer, cancellationToken);

                var serialized = new Message<TRequest>
                {
                    Payload = request
                };
                var body = serialized.ToBytes();

                var queueName = $"{Constant.RpcQueueNamePrefix}{requestedActorId}_{typeof(TRequest).AssemblyQualifiedName}";

                var props = new BasicProperties
                {
                    CorrelationId = correlationId,
                    ReplyTo = replyQueueName,
                    Expiration = messageTTL.ToString()
                };

                await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, basicProperties: props, body: body, mandatory: true, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                channel?.Dispose();
                throw new SendingException(Errors.SendingError, ex);
            }

            #endregion

            #region The response waiting and handling

            try
            {
                var timeoutTask = Task.Delay(messageTTL, cancellationToken);
                var responseTask = responseEvent.WaitAsync(cancellationToken);
                var endTask = await Task.WhenAny(responseTask, timeoutTask);

                cancellationToken.ThrowIfCancellationRequested();

                if (ReferenceEquals(responseTask, endTask))
                {
                    if (exception != null)
                        throw exception;

                    return response!;
                }

                if (channel.CloseReason != null)
                    throw new AlreadyClosedException(channel.CloseReason);

                throw new TimeoutException();
            }
            finally
            {
                channel?.Dispose();
            }

            #endregion
        }

        /// <summary>
        /// Duration of waiting for a response to a request in seconds, after which <see cref="TimeoutException"/> throws.
        /// </summary>
        public int Timeout { get; set; }

        #region IDisposable interface implementation

        bool disposed = false;

        /// <summary>
        /// Terminates the client.
        /// </summary>
        /// <param name="disposing">The indication that the termination is in progress.</param>
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
