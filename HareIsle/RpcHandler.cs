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
    public class RpcHandler<TRequest, TResponse> : IDisposable
        where TRequest: class, IValidatableObject
        where TResponse : class, IValidatableObject
    {
        public RpcHandler(IConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public void Start(string queueName, Func<TRequest, TResponse> func, ushort concurrency = 1)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));

            if (queueName == null)
                throw new ArgumentNullException(nameof(queueName));

            if (!queueName.IsValidQueueName())
                throw new ArgumentException(Errors.InvalidQueueName, nameof(queueName));

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

        public event EventHandler<InvalidRequestEventArgs>? InvalidRequest;

        public event EventHandler<SendResponseErrorEventArgs<TResponse>>? SendResponseError;

        public event EventHandler<ResponseSentEventArgs<TRequest, TResponse>>? ResponseSent;

        public event EventHandler<TRequest>? RequestHandling;

        public event EventHandler<RequestHandlingErrorEventArgs<TRequest>>? RequestHandlingError;

        public void Dispose()
        {
            if (_channel == null) return;

            if (!_channel.IsClosed)
                _channel.Close();

            _channel.Dispose();

            _channel = null;
        }

        private readonly IConnection _connection;
        private IModel? _channel;
        private Func<TRequest, TResponse>? _func;
    }
}