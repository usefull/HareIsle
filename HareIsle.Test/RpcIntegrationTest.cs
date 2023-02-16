using HareIsle.Exceptions;
using RabbitMQ.Client.Exceptions;
using System;
using System.Diagnostics;
using static HareIsle.Test.Equipment;

namespace HareIsle.Test
{
    /// <summary>
    /// <see cref="RpcClient"/> and <see cref="RpcHandler{TRequest, TResponse}"/> integration test cases.
    /// </summary>
    [TestClass]
    public class RpcIntegrationTest
    {
        /// <summary>
        /// Tests successful single RPC request.
        /// </summary>
        [TestMethod]
        public void SingleRpcSuccessTest()
        {
            var prompt = "prompt";
            var queueName = Guid.NewGuid().ToString();
            var eventHandlerReady = new AutoResetEvent(false);
            var eventFinish = new AutoResetEvent(false);

            var flagInvalidRequest = false;
            var flagRequestHandling = false;
            var flagRequestHandlingError = false;
            var flagSendResponseError = false;
            var flagResponseSent = false;

            var handlerTask = Task.Run(() =>
            {
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(CreateRabbitMqConnection());
                rpcHandler.InvalidRequest += (_, _) => flagInvalidRequest = true;
                rpcHandler.RequestHandling += (_, _) => flagRequestHandling = true;
                rpcHandler.RequestHandlingError += (_, _) => flagRequestHandlingError = true;
                rpcHandler.SendResponseError += (_, _) => flagSendResponseError = true;
                rpcHandler.ResponseSent += (_, _) => flagResponseSent = true;
                rpcHandler.Start(queueName, (request) => new TestResponse { Reply = request.Prompt!.ToUpper() });
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();

            var rpcClient = new RpcClient(CreateRabbitMqConnection());
            var requestTask = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = prompt });
            requestTask.Wait();
            var response = requestTask.Result;

            eventFinish.Set();

            handlerTask.Wait();

            Assert.AreEqual(response.Reply, prompt.ToUpper());
            Assert.IsTrue(flagRequestHandling);
            Assert.IsTrue(flagResponseSent);
            Assert.IsFalse(flagInvalidRequest);
            Assert.IsFalse(flagSendResponseError);
            Assert.IsFalse(flagRequestHandlingError);
        }

        [TestMethod]
        public async Task www()
        {
            var queueName = Guid.NewGuid().ToString();
            var eventHandlerReady = new AutoResetEvent(false);
            var eventFinish = new AutoResetEvent(false);

            var handlerTask = Task.Run(() =>
            {
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(CreateRabbitMqConnection());
                rpcHandler.RequestHandling += (_, ea) => Debug.WriteLine($"{ea.Prompt} - {DateTime.Now}");
                //rpcHandler.ResponseSent += (_, ea) => Debug.WriteLine($"<<<--- {DateTime.Now}");
                rpcHandler.Start(queueName, (request) =>
                {
                    Task.Delay(10000).Wait();
                    return new TestResponse { Reply = request.Prompt!.ToUpper() };
                }, 5);
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();
            var rpcClient = new RpcClient(CreateRabbitMqConnection()) { Timeout = 200 };

            var requestTask1 = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = "1" });
            var requestTask2 = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = "2" });
            var requestTask3 = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = "3" });
            var requestTask4 = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = "4" });
            var requestTask5 = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = "5" });
            var requestTask6 = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = "6" });
            var requestTask7 = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = "7" });
            var requestTask8 = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = "8" });
            var requestTask9 = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = "9" });
            var requestTask10 = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = "10" });

            await Task.WhenAll(requestTask1, requestTask2, requestTask3, requestTask4, requestTask5, requestTask6, requestTask7, requestTask8, requestTask9, requestTask10);
            eventFinish.Set();

            handlerTask.Wait();
        }

        /// <summary>
        /// Tests successful multiple concurrent RPC requests to the single handler.
        /// </summary>
        [TestMethod]
        public void MultipleConcurrentRequestsSuccessTest()
        {
            var queueName = Guid.NewGuid().ToString();
            var eventHandlerReady = new AutoResetEvent(false);
            var eventFinish = new AutoResetEvent(false);

            var handlerTask = Task.Run(() =>
            {
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(CreateRabbitMqConnection());
                //rpcHandler.RequestHandling += (_, _) => Debug.WriteLine($"--->>> {DateTime.Now}");
                //rpcHandler.ResponseSent += (_, _) => Debug.WriteLine($"<<<--- {DateTime.Now}");
                rpcHandler.Start(queueName, (request) => new TestResponse { Reply = request.Prompt!.ToUpper() }, 5);
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();

            var clientTask1 = Task.Run(() =>
            {                
                var rpcClient = new RpcClient(CreateRabbitMqConnection()) { Timeout = 20 };
                Enumerable.Range(0, 50).ToList().ForEach(_ =>
                {
                    var guid = Guid.NewGuid().ToString();
                    var requestTask = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = guid });
                    requestTask.Wait();
                    Assert.AreEqual(guid.ToUpper(), requestTask.Result.Reply);
                });                
            });

            var clientTask2 = Task.Run(() =>
            {                
                var rpcClient = new RpcClient(CreateRabbitMqConnection()) { Timeout = 20 };
                Enumerable.Range(0, 50).ToList().ForEach(_ =>
                {
                    var guid = Guid.NewGuid().ToString();
                    var requestTask = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = guid });
                    requestTask.Wait();
                    Assert.AreEqual(guid.ToUpper(), requestTask.Result.Reply);
                });                
            });

            var clientTask3 = Task.Run(() =>
            {                
                var rpcClient = new RpcClient(CreateRabbitMqConnection()) { Timeout = 20 };
                Enumerable.Range(0, 50).ToList().ForEach(_ =>
                {
                    var guid = Guid.NewGuid().ToString();
                    var requestTask = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = guid });
                    requestTask.Wait();
                    Assert.AreEqual(guid.ToUpper(), requestTask.Result.Reply);
                });                
            });

            try
            {
                Task.WaitAll(clientTask1, clientTask2, clientTask3);
            }
            catch
            {
                throw;
            }
            finally
            {
                eventFinish.Set();
                handlerTask.Wait();
            }
        }

        /// <summary>
        /// Tests failed request handling.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(RpcException))]
        public async Task RpcHandlingErrorTestAsync()
        {
            var errorMessage = "RPC handling error";
            var queueName = Guid.NewGuid().ToString();
            var eventHandlerReady = new AutoResetEvent(false);
            var eventFinish = new AutoResetEvent(false);
            Exception? exceptionFromEventArgs = null;
            var flagRequestHandling = false;

            var handlerTask = Task.Run(() =>
            {
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(CreateRabbitMqConnection());
                rpcHandler.RequestHandlingError += (_, ea) => exceptionFromEventArgs = ea.Exception;
                rpcHandler.RequestHandling += (_, _) => flagRequestHandling = true;
                rpcHandler.Start(queueName, (request) => throw new ApplicationException(errorMessage));
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();

            var rpcClient = new RpcClient(CreateRabbitMqConnection());

            try
            {
                var response = await rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest());
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, errorMessage);
                throw;
            }
            finally
            {
                eventFinish.Set();
                handlerTask.Wait();
            }

            Assert.IsFalse(flagRequestHandling);
            Assert.IsInstanceOfType(exceptionFromEventArgs, typeof(ApplicationException));
            Assert.AreEqual(exceptionFromEventArgs?.Message, errorMessage);
        }

        /// <summary>
        /// Tests request cancellation.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task RpcCancellationTestAsync()
        {
            var queueName = Guid.NewGuid().ToString();
            var eventHandlerReady = new AutoResetEvent(false);
            var eventFinish = new AutoResetEvent(false);

            var handlerTask = Task.Run(() =>
            {
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(CreateRabbitMqConnection());
                rpcHandler.Start(queueName, (request) =>
                {
                    Task.Delay(10000).Wait();
                    return new TestResponse();
                });
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();

            var cts = new CancellationTokenSource(5000);
            var rpcClient = new RpcClient(CreateRabbitMqConnection());

            try
            {
                var response = await rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest(), 20, cts.Token);
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                eventFinish.Set();
                handlerTask.Wait();
            }
        }

        /// <summary>
        /// Tests closing client connection while response waiting.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public void ClosingClientConnectionWnileResponseWaitingTest()
        {
            var queueName = Guid.NewGuid().ToString();
            var eventHandlerReady = new AutoResetEvent(false);
            var eventFinish = new AutoResetEvent(false);
            var eventRequestSent = new AutoResetEvent(false);

            var handlerTask = Task.Run(() =>
            {
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(CreateRabbitMqConnection());
                rpcHandler.Start(queueName, (request) =>
                {
                    Task.Delay(10000).Wait();
                    return new TestResponse();
                });
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();

            var clientConnection = CreateRabbitMqConnection();
            var clientTask = Task.Run(() =>
            {
                var rpcClient = new RpcClient(clientConnection);
                var requestTask = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest(), 20);
                eventRequestSent.Set();
                requestTask.Wait();
            });

            eventRequestSent.WaitOne();
            clientConnection.Close();

            try
            {
                clientTask.Wait();
            }
            catch (AggregateException ex)
            {
                throw (ex.InnerExceptions[0] as AggregateException)?.InnerExceptions[0] ?? new ApplicationException();
            }
            finally
            {
                eventFinish.Set();
                handlerTask.Wait();
            }
        }

        /// <summary>
        /// Tests exception in the case of performing RPC request on closed connection.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(AlreadyClosedException))]
        public async Task RpcRequestOnClosedConnectionTestAsync()
        {
            var queueName = Guid.NewGuid().ToString();
            var eventHandlerReady = new AutoResetEvent(false);
            var eventFinish = new AutoResetEvent(false);

            var handlerTask = Task.Run(() =>
            {
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(CreateRabbitMqConnection());
                rpcHandler.Start(queueName, (request) =>
                {
                    Task.Delay(10000).Wait();
                    return new TestResponse();
                });
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();

            var clientConnection = CreateRabbitMqConnection();
            clientConnection.Close();
            var rpcClient = new RpcClient(clientConnection);

            try
            {
                var response = await rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest(), 20);
            }
            catch
            {
                throw;
            }
            finally
            {
                eventFinish.Set();
                handlerTask.Wait();
            }
        }

        /// <summary>
        /// Tests the case of sending response on closed connection.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        [ExpectedException(typeof(AlreadyClosedException))]
        public async Task RpcResponseSendingOnClosedConnectionTestAsync()
        {
            var queueName = Guid.NewGuid().ToString();
            var eventHandlerReady = new AutoResetEvent(false);
            var eventFinish = new AutoResetEvent(false);

            var flagResponseSent = false;
            Exception? handlerException = null;
            var handlerConnection = CreateRabbitMqConnection();

            var handlerTask = Task.Run(() =>
            {
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(handlerConnection);
                rpcHandler.SendResponseError += (_, ea) => handlerException = ea.Exception;
                rpcHandler.ResponseSent += (_, _) => flagResponseSent = true;
                rpcHandler.Start(queueName, (request) =>
                {
                    rpcHandler.CloseChannel(false);
                    return new TestResponse();
                });
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();

            var rpcClient = new RpcClient(CreateRabbitMqConnection());
            var requestTask = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest(), 10);
            try
            {
                var response = await requestTask;
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(TimeoutException));
            }
            finally
            {
                eventFinish.Set();
                handlerTask.Wait();
            }

            Assert.IsFalse(flagResponseSent);
            throw handlerException ?? new ApplicationException();
        }
    }
}