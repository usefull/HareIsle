using HareIsle.Entities;
using HareIsle.Exceptions;
using HareIsle.Extensions;
using HareIsle.Resources;
using Microsoft.VisualStudio.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HareIsle
{
    /// <summary>
    /// RPC request client.
    /// </summary>
    public class RpcClient : IDisposable
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">An object that represents an open RabbitMQ connection.</param>
        /// <exception cref="ArgumentNullException">In the case of null connection.</exception>
        /// <exception cref="AlreadyClosedException">In the case of RPC client creating on closed connection.</exception>
        public RpcClient(IConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _channel = _connection.CreateModel();
            _replyQueueName = _channel.QueueDeclare().QueueName;

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (_, ea) =>
            {
                if (!_callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
                    return;

                var undeserializedResponse = new UndeserializedRpcResponse();
                try
                {
                    undeserializedResponse.Response = Encoding.UTF8.GetString(ea.Body.ToArray());
                }
                catch(Exception ex)
                {
                    undeserializedResponse.Exception = ex;
                }
                finally
                {
                    tcs.TrySetResult(undeserializedResponse);
                }                
            };

            _channel.BasicConsume(consumer: consumer,
                                 queue: _replyQueueName,
                                 autoAck: true);
        }

        /// <summary>
        /// Performs asynchronous RPC call.
        /// </summary>
        /// <typeparam name="TRequest">Request object type.</typeparam>
        /// <typeparam name="TResponse">Response onject type.</typeparam>
        /// <param name="queueName">Queue name that receives requests.</param>
        /// <param name="request">Request object.</param>
        /// <param name="cancellationToken">Operation cancellation token.</param>
        /// <returns>A task that represents the asynchronous calling operation, which wraps the RPC response object.</returns>
        /// <exception cref="ArgumentNullException">In the case of null queue name or request.</exception>
        /// <exception cref="ArgumentException">In the case of invalid queue name or request.</exception>
        /// <exception cref="RpcRequestSerializationException">In the case of request serialization error.</exception>
        /// <exception cref="TimeoutException">In the case of timeout expiration.</exception>
        /// <exception cref="RpcException">In the case of RPC handling error on the handler side.</exception>
        /// <exception cref="AlreadyClosedException">In the case of performing RPC request on closed connection.</exception>
        public async Task<TResponse> CallAsync<TRequest, TResponse>(string queueName, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : class, IValidatableObject
            where TResponse : class, IValidatableObject =>
            await CallAsync<TRequest, TResponse>(queueName, request, 0, cancellationToken);

        /// <summary>
        /// Performs asynchronous RPC call.
        /// </summary>
        /// <typeparam name="TRequest">Request object type.</typeparam>
        /// <typeparam name="TResponse">Response onject type.</typeparam>
        /// <param name="queueName">Queue name that receives requests.</param>
        /// <param name="request">Request object.</param>
        /// <param name="timeout">Timeout in seconds. Applies to this call only.</param>
        /// <param name="cancellationToken">Operation cancellation token.</param>
        /// <returns>A task that represents the asynchronous calling operation, which wraps the RPC response object.</returns>
        /// <exception cref="ArgumentNullException">In the case of null queue name or request.</exception>
        /// <exception cref="ArgumentException">In the case of invalid queue name or request.</exception>
        /// <exception cref="RpcRequestSerializationException">In the case of request serialization error.</exception>
        /// <exception cref="TimeoutException">In the case of timeout expiration.</exception>
        /// <exception cref="RpcException">In the case of RPC handling error on the handler side.</exception>
        /// <exception cref="AlreadyClosedException">In the case of performing RPC request on closed connection.</exception>
        public async Task<TResponse> CallAsync<TRequest, TResponse>(string queueName, TRequest request, int timeout, CancellationToken cancellationToken = default)
            where TRequest : class, IValidatableObject
            where TResponse : class, IValidatableObject
        {
            if (queueName == null)
                throw new ArgumentNullException(nameof(queueName));

            if (!queueName.IsValidQueueName())
                throw new ArgumentException(Errors.InvalidQueueName, nameof(queueName));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!request.IsValid(out var requestValidationErrors))
                throw new ArgumentException($"{Errors.InvalidRpcRequest}. {requestValidationErrors}", nameof(request));

            byte[]? bytesRequest;
            try
            {
                bytesRequest = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
            }
            catch (Exception e)
            {
                throw new RpcRequestSerializationException(Errors.RpcRequestSerializationError, e);
            }

            var props = _channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = _replyQueueName;
            var tcs = new TaskCompletionSource<UndeserializedRpcResponse>();
            _callbackMapper.TryAdd(correlationId, tcs);
            cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out _));

            _channel.BasicPublish(string.Empty, queueName, props, bytesRequest);

            var timeoutTask = Task.Delay((timeout > 0 ? timeout : _timeout) * 1000);
            var waitResponseTask = tcs.Task.WithCancellation(cancellationToken);

            var completedTask = await Task.WhenAny(timeoutTask, waitResponseTask);
            cancellationToken.ThrowIfCancellationRequested();

            if (ReferenceEquals(completedTask, timeoutTask))
                throw new TimeoutException();

            var resultException = waitResponseTask?.Result?.Exception;
            if (resultException != null)
                throw new RpcResponseDeserializationException(Errors.RpcResponseDecodingError, resultException);
            
            var strResponse = waitResponseTask?.Result?.Response;
            if (strResponse == null)
                throw new UnexpectedException(Errors.NullUndeserializedRpcResponse);

            RpcResponse<TResponse>? rpcResponse;
            try
            {
                rpcResponse = JsonSerializer.Deserialize<RpcResponse<TResponse>>(strResponse!);
            }
            catch (Exception e)
            {
                throw new RpcResponseDeserializationException(Errors.RpcResponseDeserializationError, e);
            }

            if (rpcResponse == null)
                throw new InvalidRpcResponseException(Errors.NullRpcResponse);
            else if (!rpcResponse.IsValid(out var responseErrors))
                throw new InvalidRpcResponseException($"{Errors.InvalidRpcResponse}. {responseErrors}");
            else if (!string.IsNullOrWhiteSpace(rpcResponse.Error))
                throw new RpcException(rpcResponse.Error);
            else
                return rpcResponse!.Payload!;
        }

        /// <summary>
        /// Implements <see cref="IDisposable"/> interface.
        /// </summary>
        public void Dispose()
        {
            _channel.Dispose();
        }

        /// <summary>
        /// Timeout in seconds.
        /// </summary>
        /// <remarks>Applies to calls that do not have an explicit timeout or have a timeout less than or equal to 0.</remarks>
        public int Timeout
        {
            get => _timeout;
            set => _timeout = value > 0 ? value : 15;
        }

        private int _timeout = 15;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _replyQueueName;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<UndeserializedRpcResponse>> _callbackMapper = new ConcurrentDictionary<string, TaskCompletionSource<UndeserializedRpcResponse>>();
    }
}