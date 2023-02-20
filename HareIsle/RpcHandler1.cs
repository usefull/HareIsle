using HareIsle.Entities;
using HareIsle.EventArgs;
using HareIsle.Exceptions;
using HareIsle.Extensions;
using HareIsle.Resources;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HareIsle
{
    public class RpcHandler1<TRequest, TResponse> : IDisposable
        where TRequest : class, IValidatableObject
        where TResponse : class, IValidatableObject
    {
        public RpcHandler1(IModel channel, string queueName, Func<TRequest, TResponse> func, uint funcTimeout = 1500, bool deleteQueueBeforeDispose = true)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _func = func ?? throw new ArgumentNullException(nameof(func));
            _deleteQueueBeforeDispose = deleteQueueBeforeDispose;

            if (funcTimeout < 1)
                throw new ArgumentOutOfRangeException(Errors.RequestHandlerFunctionTimeoutMustBeGraterThanZero, nameof(funcTimeout));
            else
                _funcTimeout = funcTimeout;

            if (queueName == null)
                throw new ArgumentNullException(nameof(queueName));

            if (!queueName.IsValidQueueName())
                throw new ArgumentException(Errors.InvalidQueueName, nameof(queueName));

            _queueName = queueName;

            _channel.QueueDeclare(queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnRequestReceived;
            _consumerTag = _channel.BasicConsume(queue: queueName,
                     autoAck: false,
                     consumer: consumer);
        }

        /// <summary>
        /// Processes the received request message.
        /// </summary>
        /// <param name="sender">Event sender object.</param>
        /// <param name="ea">Deliveration event data.</param>
        private void OnRequestReceived(object sender, BasicDeliverEventArgs ea)
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

                CancellationTokenSource cts = new CancellationTokenSource((int)_funcTimeout * 1000);
                var task = ExecuteHandlingAsync(request!, cts.Token);
                rpcResponse.Payload = task.Result;
                ;
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
        /// Executes the handling function for received request.
        /// </summary>
        /// <param name="request">Request object.</param>
        /// <returns>Response object.</returns>
        private async Task<TResponse?> ExecuteHandlingAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            RequestHandling?.Invoke(this, request);

            try
            {
                return await Task.Run(() => _func!(request), cancellationToken);
            }
            catch (Exception e)
            {
                RequestHandlingError?.Invoke(this, new RequestHandlingErrorEventArgs<TRequest>(request, e));
                throw;
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

        public void Dispose()
        {
            _channel.BasicCancel(_consumerTag);
            if (_deleteQueueBeforeDispose)
            {
                try
                {
                    _channel.QueueDelete(_queueName);
                }
                catch { }
            }
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

        private readonly IModel _channel;
        private readonly string _queueName;
        private readonly string _consumerTag;
        private readonly Func<TRequest, TResponse> _func;
        private readonly uint _funcTimeout;
        private readonly bool _deleteQueueBeforeDispose;
    }
}