using HareIsle.Entities;
using HareIsle.Exceptions;
using HareIsle.Extensions;
using HareIsle.Resources;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text;
using HareIsle.EventArgs;

namespace HareIsle
{
    /// <summary>
    /// RPC request handler.
    /// </summary>
    /// <typeparam name="TRequest">Request object type.</typeparam>
    /// <typeparam name="TResponse">Response object type.</typeparam>
    public class RpcHandler<TRequest, TResponse> : IDisposable
        where TRequest: class, IValidatableObject
        where TResponse : class, IValidatableObject
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">An object that represents an open RabbitMQ connection.</param>
        /// <exception cref="ArgumentNullException">In the case of null connection.</exception>
        public RpcHandler(IConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Starts listening specified queue for requests.
        /// </summary>
        /// <param name="queueName">Requests queue name.</param>
        /// <param name="func">Request handling function.</param>
        /// <param name="concurrency">The max number of requests processed simultaneously.</param>
        /// <param name="deleteQueueOnDispose">The flag instructs to delete the request queue after the disposing of the handler object.</param>
        /// <exception cref="ArgumentNullException">In the case of null queue name or func.</exception>
        /// <exception cref="ArgumentException">In the case of invalid queue name.</exception>
        public void Start(string queueName, Func<TRequest, TResponse> func, ushort concurrency = 1, bool deleteQueueOnDispose = true)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));

            if (queueName == null)
                throw new ArgumentNullException(nameof(queueName));

            if (!queueName.IsValidQueueName())
                throw new ArgumentException(Errors.InvalidQueueName, nameof(queueName));

            _deleteQueueOnDispose = deleteQueueOnDispose;
            _queueName = queueName;

            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: concurrency == 0 ? (ushort)1 : concurrency, global: false);
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnRequestReceived;
            _channel.BasicConsume(queue: queueName,
                     autoAck: false,
                     consumer: consumer);
        }

        /// <summary>
        /// Processes the received request message.
        /// </summary>
        /// <param name="model">RabbitMQ channel object.</param>
        /// <param name="ea">Deliveration event data.</param>
        private void OnRequestReceived(object model, BasicDeliverEventArgs ea)
        {
            var replyTo = ea.BasicProperties.ReplyTo;
            var correlationId = ea.BasicProperties.CorrelationId;
            var deliveryTag = ea.DeliveryTag;

            if (string.IsNullOrWhiteSpace(replyTo))
            {
                InvalidRequest?.Invoke(this, new InvalidRequestEventArgs(ea.Body, new RpcProtocolException(Errors.NoReplyTo)));
                return;
            }

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                InvalidRequest?.Invoke(this, new InvalidRequestEventArgs(ea.Body, new RpcProtocolException(Errors.NoCorrelationId)));
                return;
            }

            if (deliveryTag == 0)
            {
                InvalidRequest?.Invoke(this, new InvalidRequestEventArgs(ea.Body, new RpcProtocolException(Errors.NoDeliveryTag)));
                return;
            }

            var rpcResponse = new RpcResponse<TResponse>() { Payload = null, Error = null };
            TRequest? request = null;

            try
            {
                request = DeserializeRequest(ea);

                ValidateRequest(ea, request);

                rpcResponse.Payload = ExecuteHandling(request!);
            }
            catch (Exception e)
            {
                rpcResponse.Payload = null;
                rpcResponse.Error = e.Message;
            }
            finally
            {
                SendResponse(replyTo, correlationId, deliveryTag, request, rpcResponse);
            }
        }

        /// <summary>
        /// Executes the handling function for received request.
        /// </summary>
        /// <param name="request">Request object.</param>
        /// <returns>Response object.</returns>
        private TResponse? ExecuteHandling(TRequest request)
        {
            RequestHandling?.Invoke(this, request);

            try
            {
                return _func!(request);
            }
            catch (Exception e)
            {
                RequestHandlingError?.Invoke(this, new RequestHandlingErrorEventArgs<TRequest>(request, e));
                throw;
            }
        }

        /// <summary>
        /// Validates received request.
        /// </summary>
        /// <param name="ea">Deliveration event data.</param>
        /// <param name="request">Request object.</param>
        private void ValidateRequest(BasicDeliverEventArgs ea, TRequest? request)
        {
            try
            {
                if (request == null)
                    throw new InvalidRpcRequestException(Errors.NullRpcRequest);

                if (!request.IsValid(out var requestValidationErrors))
                    throw new InvalidRpcRequestException($"{Errors.InvalidRpcRequest}. {requestValidationErrors}");
            }
            catch (Exception e)
            {
                InvalidRequest?.Invoke(this, new InvalidRequestEventArgs(ea.Body, e));
                throw;
            }
        }

        /// <summary>
        /// Deserializes received request.
        /// </summary>
        /// <param name="ea">Deliveration event data.</param>
        /// <returns>Request object.</returns>
        private TRequest? DeserializeRequest(BasicDeliverEventArgs ea)
        {
            try
            {
                return JsonSerializer.Deserialize<TRequest>(Encoding.UTF8.GetString(ea.Body.ToArray()));
            }
            catch (Exception e)
            {
                var exception = new RpcRequestDeserializationException(Errors.RpcRequestDeserializationError, e);
                InvalidRequest?.Invoke(this, new InvalidRequestEventArgs(ea.Body, exception));
                throw exception;
            }
        }

        /// <summary>
        /// Sends response.
        /// </summary>
        /// <param name="replyTo">Reply queue name.</param>
        /// <param name="correlationId">Request correlation identity.</param>
        /// <param name="deliveryTag">Delivery tag.</param>
        /// <param name="request">Request object.</param>
        /// <param name="response">Response object.</param>
        private void SendResponse(string replyTo, string correlationId, ulong deliveryTag, TRequest? request, RpcResponse<TResponse> response)
        {
            IBasicProperties? replyProps = null;
            Exception? exception = null;
            byte[]? bytesResponse = null;

            try
            {
                replyProps = _channel!.CreateBasicProperties();
                replyProps.CorrelationId = correlationId;
                bytesResponse = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
            }
            catch (Exception e)
            {
                exception = e;
                SendResponseError?.Invoke(this, new SendResponseErrorEventArgs<TResponse>(response.Payload!, e));
            }

            try
            {
                if (exception == null)
                {
                    _channel.BasicPublish(exchange: string.Empty,
                                     routingKey: replyTo,
                                     basicProperties: replyProps,
                                     body: bytesResponse);
                }
                _channel!.BasicAck(deliveryTag: deliveryTag, multiple: false);
            }
            catch (Exception e)
            {
                if (exception == null)
                    SendResponseError?.Invoke(this, new SendResponseErrorEventArgs<TResponse>(response.Payload!, e));
                return;
            }            

            ResponseSent?.Invoke(this, new ResponseSentEventArgs<TRequest, TResponse>(request, response.Payload!));
        }

        /// <summary>
        /// Invalid request event.
        /// </summary>
        public event EventHandler<InvalidRequestEventArgs>? InvalidRequest;

        /// <summary>
        /// Failed response sending event.
        /// </summary>
        public event EventHandler<SendResponseErrorEventArgs<TResponse>>? SendResponseError;

        /// <summary>
        /// Successful response sending event.
        /// </summary>
        public event EventHandler<ResponseSentEventArgs<TRequest, TResponse>>? ResponseSent;

        /// <summary>
        /// Request processing start event.
        /// </summary>
        public event EventHandler<TRequest>? RequestHandling;

        /// <summary>
        /// Request processing error event.
        /// </summary>
        public event EventHandler<RequestHandlingErrorEventArgs<TRequest>>? RequestHandlingError;

        /// <summary>
        /// Implements <see cref="IDisposable"/> interface.
        /// </summary>
        public void Dispose()
        {
            if (_channel == null) return;

            if (!_channel.IsClosed)
            {
                if (_deleteQueueOnDispose)
                {
                    try
                    {
                        _channel.QueueDelete(_queueName);
                    }
                    catch { }
                }
                _channel.Close();
            }

            _channel.Dispose();

            _channel = null;
        }

        private readonly IConnection _connection;
        private IModel? _channel;
        private Func<TRequest, TResponse>? _func;
        private bool _deleteQueueOnDispose;
        private string? _queueName;
    }
}