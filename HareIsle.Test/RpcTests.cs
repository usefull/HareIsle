using HareIsle.Exceptions;
using HareIsle.Test.Assets;
using Microsoft.VisualStudio.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Diagnostics;

namespace HareIsle.Test
{
    [TestClass]
    public class RpcTests
    {
        private readonly string RequestedActorId = "2";

        [TestInitialize()]
        public void Initialize()
        {
            var jtf = new JoinableTaskFactory(new JoinableTaskContext());
            jtf.Run(async () =>
            {
                using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();
                await channel.QueueDeleteAsync($"{Constant.RpcQueueNamePrefix}{RequestedActorId}_{typeof(AttrRestrictObject).AssemblyQualifiedName}", false, false);
            });
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task Rpc_SuccessTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            var requests = new List<object>();
            var responses = new List<object>();
            var errors = new List<object>();

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();

            int timeout = 100;

            var handlerTask = Task.Run(async () =>
            {
                using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var handler = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req => new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber });
                handler.OnRequest += async (s, ea) => await Task.Run(() => requests.Add(ea));
                handler.OnResponse += async (s, ea) => await Task.Run(() => responses.Add(ea));
                handler.OnError += async (s, ea) => await Task.Run(() =>errors.Add(ea));
                readyEvent.Set();
                await endEvent.ToTask();
            });

            await readyEvent.ToTask();

            using var rpcClient = new RpcClient("1", connection);

            var request = new AttrRestrictObject()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };

            var response = await rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(requestedActorId: RequestedActorId, request: request, timeout: timeout);

            endEvent.Set();

            await handlerTask;

            Assert.AreEqual(response.ResultNumber, request.FirstNumber * request.SecondNumber);
            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(1, requests.Count);
            Assert.AreEqual(1, responses.Count);
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task TimeoutRpc_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();

            int timeout = 10;

            var handlerTask = Task.Run(async () =>
            {
                using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var _ = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req =>
                {
                    Task.Delay((timeout + 10) * 1000).Wait();
                    return new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                readyEvent.Set();
                await endEvent.ToTask();
            });

            await readyEvent.ToTask();

            Exception? resultException = null;

            using (var rpcClient = new RpcClient("1", connection))
            {
                var request = new AttrRestrictObject()
                {
                    FirstNumber = 2,
                    SecondNumber = 3
                };

                try
                {
                    await rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(requestedActorId: RequestedActorId, request: request, timeout: timeout);
                }
                catch (Exception e)
                {
                    resultException = e;
                }

                endEvent.Set();

                await handlerTask;
            }

            Assert.ThrowsException<TimeoutException>(() =>
            {
                if (resultException != null)
                    throw resultException;
            });
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task ParallelRpc_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();

            int timeout = 20;

            var handlerTask = Task.Run(async () =>
            {
                using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var _ = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 2, req =>
                {
                    Task.Delay(5000).Wait();

                    return new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });

                readyEvent.Set();
                await endEvent.ToTask();
            });

            await readyEvent.ToTask();

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
                await Task.WhenAll(task1, task2);

                watch.Stop();

                Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(10));

                endEvent.Set();
            }

            await handlerTask;
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task ValidateRpc_Test()
        {
            Exception? resultException = null;

            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();

            int timeout = 30;

            var handlerTask = Task.Run(async () =>
            {
                using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var _ = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req => new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber });
                readyEvent.Set();
                await endEvent.ToTask();
            });

            await readyEvent.ToTask();

            using (var rpcClient = new RpcClient("1", connection))
            {
                var request = new AttrRestrictObject()
                {
                    FirstNumber = 20,
                    SecondNumber = 3
                };

                try
                {
                    await rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(requestedActorId: RequestedActorId, request: request, timeout: timeout);
                }
                catch (Exception ex)
                {
                    resultException = ex;
                }
            }

            endEvent.Set();

            await handlerTask;

            Assert.ThrowsException<RpcHandlingException>(() =>
            {
                if (resultException != null)
                    throw resultException;
            });
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task CancelRpc_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();

            int timeout = 30;

            var handlerTask = Task.Run(async () =>
            {
                using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();

                using var _ = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req =>
                {
                    Task.Delay(TimeSpan.FromSeconds(15)).Wait();
                    return new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                readyEvent.Set();

                await endEvent.ToTask();
            });

            await readyEvent.ToTask();

            Exception exception = null;

            using (var rpcClient = new RpcClient("1", connection))
            {
                var request = new AttrRestrictObject()
                {
                    FirstNumber = 2,
                    SecondNumber = 3
                };

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));                
                try
                {
                    await rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(RequestedActorId, request, timeout, cts.Token);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                endEvent.Set();
            }

            await handlerTask;

            Assert.IsInstanceOfType<OperationCanceledException>(exception);
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task RpcClientConnectionDisposedWhileHandling_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();

            int timeout = 10;

            var handlerTask = Task.Run(async () =>
            {
                using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var _ = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req =>
                {
                    Task.Delay(5000).Wait();
                    return new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                readyEvent.Set();
                await endEvent.ToTask();
            });

            await readyEvent.ToTask();

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
                var response = await task;
            }
            catch (Exception e)
            {
                ex = e;
            }

            endEvent.Set();
            await handlerTask;

            Assert.ThrowsException<SendingException>(() =>
            {
                if (ex != null)
                    throw ex;
            });
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task RpcClientConnectionDisposedBeforeCall_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();

            int timeout = 10;

            var handlerTask = Task.Run(async () =>
            {
                await Task.Delay(5000);

                using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var _ = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req => new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber });
                readyEvent.Set();
                await endEvent.ToTask();
            });

            await readyEvent.ToTask();

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
                var response = await rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(requestedActorId: RequestedActorId, request: request, timeout: timeout);
            }
            catch (Exception e)
            {
                ex = e;
            }

            endEvent.Set();
            await handlerTask;

            Assert.ThrowsException<SendingException>(() =>
            {
                Assert.IsNotNull(ex);
                Assert.IsInstanceOfType(ex!.InnerException, typeof(ObjectDisposedException));
                throw ex;
            });           
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task RpcHandlerConnectionDisposedWhileHandling_Test()
        {
            IConnection? handlerConnection = null;
            Exception? handlerException = null;
            Exception? clientException = null;
            EventArgs.ErrorType errorType = EventArgs.ErrorType.Unknown;

            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var msgEvent = new AutoResetEvent(false);
            int timeout = 10;

            var handlerTask = Task.Run(async () =>
            {
                handlerConnection = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var handler = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, handlerConnection, 1, req =>
                {
                    msgEvent.Set();
                    Task.Delay(5000).Wait();
                    return new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                handler.OnError += async (s, ea) => await Task.Run(() =>
                {
                    handlerException = ea.Exception;
                    errorType = ea.Type;
                });

                readyEvent.Set();
                await endEvent.ToTask();
            });

            await readyEvent.ToTask();

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();
            using var rpcClient = new RpcClient("1", connection);
            var request = new AttrRestrictObject()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };
            var callTask = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(RequestedActorId, request, timeout);
            msgEvent.WaitOne();
            handlerConnection?.Dispose();

            try
            {
                await callTask;
            }
            catch (Exception e)
            {
                clientException = e;
            }

            endEvent.Set();
            await handlerTask;

            Assert.ThrowsException<AlreadyClosedException>(() =>
            {
                Assert.IsInstanceOfType<TimeoutException>(clientException);
                Assert.AreEqual(EventArgs.ErrorType.Sending, errorType);
                if (handlerException != null)
                    throw handlerException;
            });
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task CreationRpcHandlerWithClosedConnection_Test()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            {
                using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                conn.Dispose();
                using var _ = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req => new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber });
            });
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task CreationRpcHandlerWithNonEmptyQueue_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var msgEvent = new AutoResetEvent(false);
            int timeout = 15;

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();

            using var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync(queue: $"{Constant.RpcQueueNamePrefix}{RequestedActorId}_{typeof(AttrRestrictObject).AssemblyQualifiedName}", durable: false, exclusive: true, autoDelete: false, arguments: null);

            using var rpcClient = new RpcClient("1", connection);

            var request = new AttrRestrictObject()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };
            var callTask = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(RequestedActorId, request, timeout);
            var timerTask = Task.Delay(TimeSpan.FromSeconds(1));

            var t = await Task.WhenAny(callTask, timerTask);
            Assert.IsTrue(ReferenceEquals(t, timerTask));

            var handlerTask = Task.Delay(2000).ContinueWith(async _ =>
            {
                using var handler = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, connection, 1, req =>
                {
                    msgEvent.Set();
                    return new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                await endEvent.ToTask();
            });

            await msgEvent.ToTask();
            endEvent.Set();
            await handlerTask;
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task RpcCallExpiration_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var msgEvent = new AutoResetEvent(false);
            int timeout = 10;

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync(queue: $"{Constant.RpcQueueNamePrefix}{RequestedActorId}_{typeof(AttrRestrictObject).AssemblyQualifiedName}", durable: false, exclusive: true, autoDelete: false, arguments: null);

            using var rpcClient = new RpcClient("1", connection);

            var request = new AttrRestrictObject()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };

            var callTask = rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(RequestedActorId, request, timeout);
            var timerTask = Task.Delay(TimeSpan.FromSeconds(1));
            var t = await Task.WhenAny(callTask, timerTask);
            Assert.IsTrue(ReferenceEquals(t, timerTask));

            var handlerTask = Task.Delay(TimeSpan.FromSeconds(11)).ContinueWith(async _ =>
            {
                using var handler = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, connection, 1, req =>
                {
                    msgEvent.Set();
                    return new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber };
                });
                await endEvent.ToTask();
            });
            Assert.IsFalse(await msgEvent.ToTask(20000));

            endEvent.Set();
            await handlerTask;
        }

        /// <summary>
        /// This test requires manually disabling/enabling the network.
        /// Run only in debug mode by commenting out the [Ignore] attribute
        /// and placing breakpoints at the places where the network is turned on / off
        /// (see comments into the test method body).
        /// </summary>
        [Ignore]
        [TestMethod]
        public async Task SuccessRpcAfterConnectionRecoveryTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var stoppedEven = new AutoResetEvent(false);
            var needToStartedSwitchOnEvent = new AutoResetEvent(false);
            var startedEven = new AutoResetEvent(false);

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();

            int timeout = 100;

            var handlerTask = Task.Run(async () =>
            {
                using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var handler = new RpcHandler<AttrRestrictObject, RpcTestResult>(RequestedActorId, conn, 1, req => new RpcTestResult() { ResultNumber = req.FirstNumber * req.SecondNumber });

                handler.Stopped += async (s, ev) => await Task.Run(() => stoppedEven.Set());

                readyEvent.Set();
                await needToStartedSwitchOnEvent.ToTask();
                handler.Started += async (s, ev) => await Task.Run(() => startedEven.Set());

                await endEvent.ToTask();
            });

            await readyEvent.ToTask();

            using var rpcClient = new RpcClient("1", connection);

            var request = new AttrRestrictObject()
            {
                FirstNumber = 2,
                SecondNumber = 3
            };

            var response = await rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(requestedActorId: RequestedActorId, request: request, timeout: timeout);

            needToStartedSwitchOnEvent.Set();

            //here you need to manually TURN OFF the network
            ;
            await stoppedEven.ToTask();

            try
            {
                var lostRequest = new AttrRestrictObject()
                {
                    FirstNumber = 4,
                    SecondNumber = 6
                };

                await rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(RequestedActorId, lostRequest, timeout);
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType<SendingException>(ex);
                Assert.IsInstanceOfType<AlreadyClosedException>(ex.InnerException);
            }

            //here you need to manually TURN ON the network
            ;
            await startedEven.ToTask();

            var request1 = new AttrRestrictObject()
            {
                FirstNumber = 3,
                SecondNumber = 9
            };
            var response1 = await rpcClient.CallAsync<AttrRestrictObject, RpcTestResult>(requestedActorId: RequestedActorId, request: request1, timeout: timeout);

            endEvent.Set();

            Assert.AreEqual(response.ResultNumber, request.FirstNumber * request.SecondNumber);
            Assert.AreEqual(response1.ResultNumber, request1.FirstNumber * request1.SecondNumber);

            await handlerTask;
        }
    }
}