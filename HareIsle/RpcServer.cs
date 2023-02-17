using HareIsle.Extensions;
using HareIsle.Resources;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HareIsle
{
    public class RpcServer : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public RpcServer(IConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

            _ = Task.Run(MainFunction);
        }

        public RpcServer Method<TRequest, TResponse>(string queueName, Func<TRequest, TResponse> method)
            //where TRequest : class, IValidatableObject
            //where TResponse : class, IValidatableObject
        {
            if (queueName == null)
                throw new ArgumentNullException(nameof(queueName));

            if (!queueName.IsValidQueueName())
                throw new ArgumentException(Errors.InvalidQueueName, nameof(queueName));

            if (method == null)
                throw new ArgumentNullException(nameof(method));

            _methods.Add(queueName, method.Method);
            
            return this;
        }

        public void Test(string queueName)
        {
            var mi = _methods[queueName];
            var ddd = Delegate.CreateDelegate(typeof(Func<int, string>), mi);
            //var res = _methods[queueName].Invoke(_methods[queueName], new[] { (object)10 });
            ;
        }

        private void MainFunction()
        {



            _mainFuncFinished.Set();
        }

        public void Dispose()
        {
            _disposed.Set();
            _mainFuncFinished.WaitOne();
            _channel?.Dispose();            
        }

        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly AutoResetEvent _disposed = new AutoResetEvent(false);
        private readonly AutoResetEvent _mainFuncFinished = new AutoResetEvent(false);
        private readonly Dictionary<string, MethodInfo> _methods = new Dictionary<string, MethodInfo>();
        //private readonly ConcurrentQueue<BasicDeliverEventArgs> _deliverQueue = new ConcurrentQueue<BasicDeliverEventArgs>();
    }
}
