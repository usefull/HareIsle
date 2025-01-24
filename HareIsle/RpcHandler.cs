using HareIsle.Entities;
using HareIsle.Exceptions;
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
    /// The RPC-query handler.
    /// </summary>
    /// <typeparam name="TRequest">The RPC-query type.</typeparam>
    /// <typeparam name="TResponse">The RPC-response type.</typeparam>
    public class RpcHandler<TRequest, TResponse> : BaseHandler
        where TRequest : class, IValidatableObject
        where TResponse : class, IValidatableObject
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="actorId">The actor ID.</param>
        /// <param name="connection">The RabbitMQ connection.</param>
        /// <param name="concurrency">The number of requests processed simultaneously.</param>
        /// <param name="func">The RPC-query handler function.</param>
        /// <exception cref="SubscriptionException">In case of an error during the process of starting listening to requests.</exception>
        /// <exception cref="ArgumentNullException">In case of null has passed into <paramref name="connection"/> or <paramref name="func"/> params.</exception>
        /// <exception cref="ArgumentOutOfRangeException">In case of the <paramref name="concurrency"/> param is out of range.</exception>
        public RpcHandler(string actorId, IConnection connection, int concurrency, Func<TRequest, TResponse> func)
            : base(actorId, connection)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (concurrency < 1 || concurrency > 10)
                throw new ArgumentOutOfRangeException(nameof(concurrency), concurrency, Errors.ConcurrentCustomersOutOfRange);

            Func = func ?? throw new ArgumentNullException(nameof(func));

            QueueName = $"{Constant.RpcQueueNamePrefix}{actorId}_{typeof(TRequest).AssemblyQualifiedName}";

            Exception? ex = null;
            var jtf = new JoinableTaskFactory(new JoinableTaskContext());
            jtf.Run(async () =>
            {
                try
                {
                    Channel = await Connection.CreateChannelAsync();
                    await Channel.QueueDeclareAsync(queue: QueueName, durable: false, exclusive: true, autoDelete: false, arguments: null);
                    await Channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

                    for (int i = 0; i < concurrency; i++)
                    {
                        var consumer = CreateConsumer();
                        consumer.ReceivedAsync += OnRecievedAsync;
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
                throw new SubscriptionException(string.Format(Errors.SubscriptionError, QueueName), ex);
            }
        }

        /// <summary>
        /// Handles an event of RPC-query receiving.
        /// </summary>
        /// <param name="model">The RabbitMQ channel.</param>
        /// <param name="ea">Event arguments.</param>
        private async Task OnRecievedAsync(object? model, BasicDeliverEventArgs ea)
        {
            Message<TRequest>? msgRequest = null;
            var msgResponse = new Message<IValidatableObject>();
            byte[]? incomingBytes = null;

            try
            {
                incomingBytes = ea.Body.ToArray();

                try
                {
                    msgRequest = Message<TRequest>.FromBytes(incomingBytes);
                }
                catch (Exception e)
                {
                    if (OnError != null)
                        await OnError.InvokeAsync(this, new EventArgs.ErrorEventArgs<TRequest, TResponse>
                        {
                            Type = EventArgs.ErrorType.Deserializing,
                            ActorId = ActorId,
                            Exception = e,
                            RawIncoming = incomingBytes
                        });
                    await Channel!.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    return;
                }

                try
                {
                    msgRequest.ThrowIfInvalid();
                }
                catch (Exception e)
                {
                    if (OnError != null)
                        await OnError.InvokeAsync(this, new EventArgs.ErrorEventArgs<TRequest, TResponse>
                        {
                            Type = EventArgs.ErrorType.Validating,
                            ActorId = ActorId,
                            Exception = e,
                            Incoming = msgRequest.Payload
                        });
                    msgResponse.Payload = new RpcHandlingError
                    {
                        ErrorMessage = Errors.InvalidRpcRequest
                    };
                }

                if (msgResponse.Payload == null)
                {
                    if (OnRequest != null)
                        await OnRequest.InvokeAsync(this, new EventArgs.IncomingEventArgs<TRequest> { ActorId = ActorId, Incoming = msgRequest.Payload });

                    try
                    {
                        msgResponse.Payload = Func?.Invoke(msgRequest.Payload!);
                    }
                    catch (Exception e)
                    {
                        if (OnError != null)
                            await OnError.InvokeAsync(this, new EventArgs.ErrorEventArgs<TRequest, TResponse>
                            {
                                Type = EventArgs.ErrorType.Handling,
                                ActorId = ActorId,
                                Exception = e,
                                Incoming = msgRequest.Payload
                            });
                        msgResponse.Payload = new RpcHandlingError
                        {
                            ErrorMessage = e.Message
                        };
                    }
                }

                byte[] responseBytes;
                try
                {
                    responseBytes = msgResponse.ToBytes();
                }
                catch (Exception ex)
                {
                    if (OnError != null)
                        await OnError.InvokeAsync(this, new EventArgs.ErrorEventArgs<TRequest, TResponse>
                        {
                            Type = EventArgs.ErrorType.Serializing,
                            ActorId = ActorId,
                            Exception = ex,
                            Incoming = msgRequest.Payload,
                            Outgoing = (TResponse)msgResponse.Payload
                        });
                    await Channel!.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    return;
                }

                try
                {
                    var replyProps = new BasicProperties
                    {
                        CorrelationId = ea.BasicProperties.CorrelationId
                    };

                    await Channel!.BasicPublishAsync(exchange: string.Empty, routingKey: ea.BasicProperties.ReplyTo, basicProperties: replyProps, body: responseBytes, mandatory: true);
                    if (OnResponse != null)
                        await OnResponse.InvokeAsync(this, new EventArgs.OutgoingEventArgs<TRequest, TResponse> { ActorId = ActorId, Incoming = msgRequest.Payload, Outgoing = (TResponse)msgResponse.Payload });
                }
                catch (Exception e)
                {
                    if (OnError != null)
                        await OnError.InvokeAsync(this, new EventArgs.ErrorEventArgs<TRequest, TResponse>
                        {
                            Type = EventArgs.ErrorType.Sending,
                            ActorId = ActorId,
                            Exception = e,
                            Incoming = msgRequest.Payload,
                            Outgoing = (TResponse)msgResponse.Payload
                        });
                }
                finally
                {
                    if (Channel!.IsOpen)
                        await Channel!.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            }
            catch (Exception e)
            {
                if (OnError != null)
                    await OnError.InvokeAsync(this, new EventArgs.ErrorEventArgs<TRequest, TResponse>
                    {
                        Type = EventArgs.ErrorType.Acking,
                        ActorId = ActorId,
                        Exception = e,
                        RawIncoming = incomingBytes,
                        Incoming = msgRequest != null ? msgRequest.Payload : default,
                        Outgoing = (TResponse)msgResponse.Payload!
                    });
            }
        }

        /// <summary>
        /// Fires when an error occured while handling a request.
        /// </summary>
        public event Microsoft.VisualStudio.Threading.AsyncEventHandler<EventArgs.ErrorEventArgs<TRequest, TResponse>>? OnError;

        /// <summary>
        /// Fires when a request has received.
        /// </summary>
        public event Microsoft.VisualStudio.Threading.AsyncEventHandler<EventArgs.IncomingEventArgs<TRequest>>? OnRequest;

        /// <summary>
        /// Fires when a response has sent.
        /// </summary>
        public event Microsoft.VisualStudio.Threading.AsyncEventHandler<EventArgs.OutgoingEventArgs<TRequest, TResponse>>? OnResponse;

        /// <summary>
        /// The RPC-query handler function.
        /// </summary>
        protected Func<TRequest, TResponse>? Func { get; set; }

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
                    c.ReceivedAsync -= OnRecievedAsync;
            }

            disposed = true;
            base.Dispose(disposing);
        }

        #endregion
    }
}