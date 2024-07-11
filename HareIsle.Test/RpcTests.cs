using HareIsle.Entities;
using HareIsle.Exceptions;
using HareIsle.Test.Assets;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HareIsle.Test
{
    [TestClass]
    public class RpcTests
    {
        private readonly string RequestedActorId = "2";

        [TestInitialize()]
        public void Initialize()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDelete($"{Constant.RpcQueueNamePrefix}{RequestedActorId}_{typeof(AttrRestrictObject).AssemblyQualifiedName}", false, false);
        }

        [TestMethod]
        [Timeout(30000)]
        public void Rpc_SuccessTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            var requests = new List<object>();
            var responses = new List<object>();
            var errors = new List<object>();

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 100;

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var handler = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req => new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber });
                handler.OnRequest += (s, ea) => requests.Add(ea);
                handler.OnResponse += (s, ea) => responses.Add(ea);
                handler.OnError += (s, ea) => errors.Add(ea);
                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using var rpcClient = new RpcClient("1", connection);

            var request = new AttrRestrictObject()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };

            var task = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(requestedActorId: RequestedActorId, request: request, timeout: timeout);
            task.Wait();
            var response = task.Result;

            endEvent.Set();

            handlerTask.Wait();

            Assert.AreEqual(response.ResultNumber, request.FirstNumber * request.SecondNumber);
            Assert.AreEqual(errors.Count, 0);
            Assert.AreEqual(requests.Count, 1);
            Assert.AreEqual(responses.Count, 1);
        }

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(TimeoutException))]
        public void TimeoutRpc_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 10;

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var _ = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req =>
                {
                    Task.Delay((timeout + 10) * 1000).Wait();
                    return new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            Exception? resultException = null;

            using (var rpcClient = new RpcClient("1", connection))
            {

                var request = new AttrRestrictObject()
                {
                    FirstNumber = 2,
                    SecondNumber = 3
                };

                Exception? ex = null;
                var task = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(requestedActorId: RequestedActorId, request: request, timeout: timeout);
                try
                {
                    task.Wait();
                }
                catch (Exception e)
                {
                    resultException = e.InnerException;
                }

                endEvent.Set();

                handlerTask.Wait();
            }

            if (resultException != null)
                throw resultException;
        }

        [TestMethod]
        [Timeout(30000)]
        public void ParallelRpc_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 20;

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var _ = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 2, req =>
                {
                    Task.Delay(5000).Wait();

                    return new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });

                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using (var rpcClient = new RpcClient("1", connection))
            {
                var request1 = new AttrRestrictObject()
                {
                    FirstNumber = 1,
                    SecondNumber = 3
                };
                var request2 = new AttrRestrictObject()
                {
                    FirstNumber = 2,
                    SecondNumber = 3
                };

                var watch = new Stopwatch();
                watch.Start();

                var task1 = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(requestedActorId: RequestedActorId, request: request1, timeout: timeout);
                var task2 = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(requestedActorId: RequestedActorId, request: request2, timeout: timeout);
                Task.WaitAll(task1, task2);

                watch.Stop();

                Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(10));

                endEvent.Set();
            }

            handlerTask.Wait();
        }

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(RpcHandlingException))]
        public void ValidateRpc_Test()
        {
            Exception? resultException = null;

            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 30;

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var _ = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req => new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber });
                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using (var rpcClient = new RpcClient("1", connection))
            {

                var request = new AttrRestrictObject()
                {
                    FirstNumber = 20,
                    SecondNumber = 3
                };

                var task = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(requestedActorId: RequestedActorId, request: request, timeout: timeout);
                try
                {
                    task.Wait();
                }
                catch (Exception ex)
                {
                    resultException = ex.InnerException;
                }
            }

            endEvent.Set();

            handlerTask.Wait();

            if (resultException != null)
                throw resultException;
        }

        [TestMethod]
        [Timeout(30000)]
        public void CancelRpc_Test()
        {
            Task<RpcTestResult>? rpcCallTask = null;

            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 30;

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();

                using var _ = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req =>
                {
                    Task.Delay(TimeSpan.FromSeconds(15)).Wait();
                    return new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                readyEvent.Set();

                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using (var rpcClient = new RpcClient("1", connection))
            {

                var request = new AttrRestrictObject()
                {
                    FirstNumber = 2,
                    SecondNumber = 3
                };

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                rpcCallTask = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(RequestedActorId, request, timeout, cts.Token);
                try
                {
                    rpcCallTask.Wait();
                }
                catch { }
                endEvent.Set();
            }

            handlerTask.Wait();

            Assert.IsTrue(rpcCallTask.IsCanceled);
        }

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(AlreadyClosedException))]
        public void RpcClientConnectionDisposedWhileHandling_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 10;

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var _ = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req =>
                {
                    Task.Delay(5000).Wait();
                    return new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using var rpcClient = new RpcClient("1", connection);

            var request = new AttrRestrictObject()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };

            var task = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(requestedActorId: RequestedActorId, request: request, timeout: timeout);
            connection.Dispose();

            Exception? ex = null;

            try
            {
                task.Wait();
                var response = task.Result;
            }
            catch (Exception e)
            {
                ex = e.InnerException;
            }

            endEvent.Set();
            handlerTask.Wait();

            throw ex;
        }

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(SendingException))]
        public void RpcClientConnectionDisposedBeforeCall_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 10;

            var handlerTask = Task.Run(async () =>
            {
                await Task.Delay(5000);

                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var _ = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req => new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber });
                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using var rpcClient = new RpcClient("1", connection);

            var request = new AttrRestrictObject()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };

            connection.Dispose();

            Exception? ex = null;

            try
            {
                var task = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(requestedActorId: RequestedActorId, request: request, timeout: timeout);
                task.Wait();
                var response = task.Result;
            }
            catch (Exception e)
            {
                ex = e.InnerException;
            }

            endEvent.Set();
            handlerTask.Wait();

            Assert.IsInstanceOfType(ex.InnerException, typeof(ObjectDisposedException));
            throw ex;
        }

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(AlreadyClosedException))]
        public void RpcHandlerConnectionDisposedWhileHandling_Test()
        {
            IConnection handlerConnection = null;
            Exception handlerException = null;
            Exception clientException = null;
            EventArgs.ErrorType errorType = EventArgs.ErrorType.Unknown;

            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var msgEvent = new AutoResetEvent(false);
            int timeout = 10;

            var handlerTask = Task.Run(() =>
            {
                handlerConnection = Env.RabbitConnectionFactory.CreateConnection();
                using var handler = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, handlerConnection, 1, req =>
                {
                    msgEvent.Set();
                    Task.Delay(5000).Wait();
                    return new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                handler.OnError += (s, ea) =>
                {
                    handlerException = ea.Exception;
                    errorType = ea.Type;
                };

                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using var rpcClient = new RpcClient("1", connection);
            var request = new AttrRestrictObject()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };
            var callTask = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(RequestedActorId, request, timeout);
            msgEvent.WaitOne();
            handlerConnection.Dispose();

            try
            {
                callTask.Wait();
            }
            catch (Exception e)
            {
                clientException = e.InnerException;
            }

            endEvent.Set();
            handlerTask.Wait();

            Assert.IsInstanceOfType(clientException, typeof(TimeoutException));
            Assert.AreEqual(errorType, EventArgs.ErrorType.Sending);
            if (handlerException != null)
                throw handlerException;
        }

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(ArgumentException))]
        public void CreationRpcHandlerWithClosedConnection_Test()
        {
            using var conn = Env.RabbitConnectionFactory.CreateConnection();
            conn.Dispose();
            using var _ = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req => new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber });
        }

        [TestMethod]
        [Timeout(30000)]
        public void CreationRpcHandlerWithNonEmptyQueue_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var msgEvent = new AutoResetEvent(false);
            int timeout = 15;

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            //{
            using var channel = connection.CreateModel();
            channel.QueueDeclare(queue: $"{Constant.RpcQueueNamePrefix}{RequestedActorId}_{typeof(AttrRestrictObject).AssemblyQualifiedName}", durable: false, exclusive: true, autoDelete: false, arguments: null);

            using var rpcClient = new RpcClient("1", connection);

            var request = new AttrRestrictObject()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };
            var callTask = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(RequestedActorId, request, timeout);

            Assert.IsFalse(callTask.Wait(TimeSpan.FromSeconds(1)));
            //}

            var handlerTask = Task.Delay(2000).ContinueWith(_ =>
            {
                using var handler = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, connection, 1, req =>
                {
                    msgEvent.Set();
                    return new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                endEvent.WaitOne();
            });

            msgEvent.WaitOne();
            endEvent.Set();
            handlerTask.Wait();
        }

        [TestMethod]
        [Timeout(30000)]
        public void RpcCallExpiration_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var msgEvent = new AutoResetEvent(false);
            int timeout = 10;

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDeclare(queue: $"{Constant.RpcQueueNamePrefix}{RequestedActorId}_{typeof(AttrRestrictObject).AssemblyQualifiedName}", durable: false, exclusive: true, autoDelete: false, arguments: null);

            using var rpcClient = new RpcClient("1", connection);

            var request = new AttrRestrictObject()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };

            var callTask = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(RequestedActorId, request, timeout);

            Assert.IsFalse(callTask.Wait(TimeSpan.FromSeconds(1)));

            var handlerTask = Task.Delay(TimeSpan.FromSeconds(11)).ContinueWith(_ =>
            {
                using var handler = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, connection, 1, req =>
                {
                    msgEvent.Set();
                    return new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                endEvent.WaitOne();
            });
            Assert.IsFalse(msgEvent.WaitOne(TimeSpan.FromSeconds(20)));

            endEvent.Set();
            handlerTask.Wait();
        }

        /// <summary>
        /// This test requires manually disabling/enabling the network.
        /// Run only in debug mode by commenting out the [Ignore] attribute
        /// and placing breakpoints at the places where the network is turned on / off
        /// (see comments into the test method body).
        /// </summary>
        [Ignore]
        [TestMethod]
        public void SuccessRpcAfterConnectionRecoveryTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var stoppedEven = new AutoResetEvent(false);
            var needToStartedSwitchOnEvent = new AutoResetEvent(false);
            var startedEven = new AutoResetEvent(false);

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            int timeout = 100;

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var handler = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req => new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber });

                handler.Stopped += (s, ev) => stoppedEven.Set();

                readyEvent.Set();
                needToStartedSwitchOnEvent.WaitOne();
                handler.Started += (s, ev) => startedEven.Set();

                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using var rpcClient = new RpcClient("1", connection);

            var request = new AttrRestrictObject()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };

            var task = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(requestedActorId: RequestedActorId, request: request, timeout: timeout);
            task.Wait();
            var response = task.Result;

            needToStartedSwitchOnEvent.Set();

            //here you need to manually TURN OFF the network
            ;
            stoppedEven.WaitOne();

            try
            {
                var lostRequest = new AttrRestrictObject()
                {
                    FirstNumber = 4,
                    SecondNumber = 6
                };

                var lostTask = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(RequestedActorId, lostRequest, timeout);
                lostTask.Wait();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(SendingException));
                Assert.IsInstanceOfType(ex.InnerException.InnerException, typeof(AlreadyClosedException));
            }

            //here you need to manually TURN ON the network
            ;
            startedEven.WaitOne();

            var request1 = new AttrRestrictObject()
            {
                FirstNumber = 3,
                SecondNumber = 9
            };
            var task1 = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(requestedActorId: RequestedActorId, request: request1, timeout: timeout);
            task1.Wait();
            var response1 = task1.Result;

            endEvent.Set();

            Assert.AreEqual(response.ResultNumber, request.FirstNumber * request.SecondNumber);
            Assert.AreEqual(response1.ResultNumber, request1.FirstNumber * request1.SecondNumber);

            handlerTask.Wait();
        }
    }
}
