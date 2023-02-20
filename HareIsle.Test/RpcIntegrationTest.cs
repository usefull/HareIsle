using HareIsle.Exceptions;
using RabbitMQ.Client.Exceptions;
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
                using var conn = CreateRabbitMqConnection();
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(conn);
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

            using var conn = CreateRabbitMqConnection();
            using var rpcClient = new RpcClient(conn);
            var requestTask = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = prompt }, 500);
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

        /// <summary>
        /// Tests successful multiple concurrent RPC requests to the single handler.
        /// </summary>
        [TestMethod]
        public void ManyClientsSingleHandlerTest()
        {
            var queueName = Guid.NewGuid().ToString();
            var eventHandlerReady = new AutoResetEvent(false);
            var eventFinish = new AutoResetEvent(false);

            var handlerTask = Task.Run(() =>
            {
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(CreateRabbitMqConnection());
                rpcHandler.RequestHandling += (_, _) => Debug.WriteLine($"--->>> {DateTime.Now}");
                rpcHandler.ResponseSent += (_, _) => Debug.WriteLine($"<<<--- {DateTime.Now}");
                rpcHandler.Start(queueName, (request) => new TestResponse { Reply = request.Prompt!.ToUpper() }, 5);
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();

            var clientTask1 = Task.Run(() =>
            {
                using var conn = CreateRabbitMqConnection();
                using var rpcClient = new RpcClient(conn) { Timeout = 20 };
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
                using var conn = CreateRabbitMqConnection();
                using var rpcClient = new RpcClient(conn) { Timeout = 20 };
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
                using var conn = CreateRabbitMqConnection();
                using var rpcClient = new RpcClient(conn) { Timeout = 20 };
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
                //using var conn = CreateRabbitMqConnection();
                //using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(conn);
                //rpcHandler.RequestHandlingError += (_, ea) => exceptionFromEventArgs = ea.Exception;
                //rpcHandler.RequestHandling += (_, _) => flagRequestHandling = true;
                //rpcHandler.Start(queueName, (request) => throw new ApplicationException(errorMessage));
                //eventHandlerReady.Set();
                //eventFinish.WaitOne();

                using var conn = CreateRabbitMqConnection();
                using var channel = conn.CreateModel();
                using var rpcHandler = new RpcHandler1<TestRequest, TestResponse>(channel, queueName, (request) => /*{ Thread.Sleep(5000); return new TestResponse(); }*/ throw new ApplicationException(errorMessage));
                rpcHandler.RequestHandlingError += (_, ea) => exceptionFromEventArgs = ea.Exception;
                rpcHandler.RequestHandling += (_, _) => flagRequestHandling = true;
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();

            using var conn = CreateRabbitMqConnection();
            using var rpcClient = new RpcClient(conn);

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
                await handlerTask;
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
                using var conn = CreateRabbitMqConnection();
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(conn);
                rpcHandler.Start(queueName, (request) =>
                {
                    Task.Delay(10000).Wait();
                    return new TestResponse();
                });
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();

            var conn = CreateRabbitMqConnection();
            var cts = new CancellationTokenSource(5000);
            using var rpcClient = new RpcClient(conn);

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
                await handlerTask;
                conn.Dispose();
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
                using var conn = CreateRabbitMqConnection();
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(conn);
                rpcHandler.Start(queueName, (request) =>
                {
                    Task.Delay(10000).Wait();
                    return new TestResponse();
                });
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();

            using var clientConnection = CreateRabbitMqConnection();
            var clientTask = Task.Run(() =>
            {
                using var rpcClient = new RpcClient(clientConnection);
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
            using var handlerConnection = CreateRabbitMqConnection();

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

            using var conn = CreateRabbitMqConnection();
            using var rpcClient = new RpcClient(conn);
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
                await handlerTask;
            }

            Assert.IsFalse(flagResponseSent);
            throw handlerException ?? new ApplicationException();
        }

        /// <summary>
        /// Tests integration of single client and three of the same handlers.
        /// </summary>
        [TestMethod]
        public void SingleClientMultupleSameHandlersTest()
        {
            var queueName = Guid.NewGuid().ToString();
            var eventHandlerReady1 = new AutoResetEvent(false);
            var eventFinish1 = new AutoResetEvent(false);
            var eventHandlerReady2 = new AutoResetEvent(false);
            var eventFinish2 = new AutoResetEvent(false);
            var eventHandlerReady3 = new AutoResetEvent(false);
            var eventFinish3 = new AutoResetEvent(false);

            var handlerTask1 = Task.Run(() =>
            {
                using var conn = CreateRabbitMqConnection();
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(conn);
                rpcHandler.RequestHandling += (_, _) => Debug.WriteLine($"{DateTime.Now} - 1");
                rpcHandler.Start(queueName, (request) =>
                {
                    Task.Delay(3000).Wait();
                    return new TestResponse { Reply = "1" };
                }, 15);
                eventHandlerReady1.Set();
                eventFinish1.WaitOne();
            });

            var handlerTask2 = Task.Run(() =>
            {
                using var conn = CreateRabbitMqConnection();
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(conn);
                rpcHandler.RequestHandling += (_, _) => Debug.WriteLine($"{DateTime.Now} - 2");
                rpcHandler.Start(queueName, (request) =>
                {
                    Task.Delay(3000).Wait();
                    return new TestResponse { Reply = "2" };
                }, 5);
                eventHandlerReady2.Set();
                eventFinish2.WaitOne();
            });

            var handlerTask3 = Task.Run(() =>
            {
                using var conn = CreateRabbitMqConnection();
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(conn);
                rpcHandler.RequestHandling += (_, _) => Debug.WriteLine($"{DateTime.Now} - 3");
                rpcHandler.Start(queueName, (request) =>
                {
                    Task.Delay(3000).Wait();
                    return new TestResponse { Reply = "3" };
                });
                eventHandlerReady3.Set();
                eventFinish3.WaitOne();
            });
            
            WaitHandle.WaitAll(new[] { eventHandlerReady1, eventHandlerReady2, eventHandlerReady3 });

            using var conn = CreateRabbitMqConnection();
            var rpcTasks = new List<Task<TestResponse>>();
            using (var rpcClient = new RpcClient(conn))
            {
                Enumerable.Range(0, 50).ToList().ForEach(_ =>
                {
                    rpcTasks.Add(rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest(), 200));
                });
                Task.WaitAll(rpcTasks.ToArray());
            }

            var responses = rpcTasks.Select(t => t.Result.Reply).ToList();
            var r1 = responses.Count(r => r == "1");
            var r2 = responses.Count(r => r == "2");
            var r3 = responses.Count(r => r == "3");

            Assert.IsTrue(r1 > 0);
            Assert.IsTrue(r2 > 0);
            Assert.IsTrue(r3 > 0);

            Assert.IsTrue(r1 > r2);
            Assert.IsTrue(r2 > r3);

            var p1 = r1 * 100 / responses.Count;
            var p2 = r2 * 100 / responses.Count;
            var p3 = r3 * 100 / responses.Count;

            eventFinish1.Set();
            eventFinish2.Set();
            eventFinish3.Set();
            Task.WaitAll(handlerTask1, handlerTask2, handlerTask3);
        }

        /// <summary>
        /// Tests integration of single client and two different handlers.
        /// </summary>
        [TestMethod]
        public async Task SingleClientMultupleDifferentHandlersTestAsync()
        {
            var queueName1 = Guid.NewGuid().ToString();
            var queueName2 = Guid.NewGuid().ToString();
            var eventHandlerReady1 = new AutoResetEvent(false);
            var eventFinish1 = new AutoResetEvent(false);
            var eventHandlerReady2 = new AutoResetEvent(false);
            var eventFinish2 = new AutoResetEvent(false);

            var handlerTask1 = Task.Run(() =>
            {
                using var conn = CreateRabbitMqConnection();
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(conn);
                rpcHandler.Start(queueName1, (request) =>
                {
                    Task.Delay(3000).Wait();
                    return new TestResponse { Reply = request.Prompt.ToUpper() };
                });
                eventHandlerReady1.Set();
                eventFinish1.WaitOne();
            });

            var handlerTask2 = Task.Run(() =>
            {
                using var conn = CreateRabbitMqConnection();
                using var rpcHandler = new RpcHandler<FakeRequest, FakeResponse>(conn);
                rpcHandler.Start(queueName2, (request) =>
                {
                    Task.Delay(3000).Wait();
                    return new FakeResponse { Total = (request.Quantity * 2).ToString() };
                });
                eventHandlerReady2.Set();
                eventFinish2.WaitOne();
            });

            WaitHandle.WaitAll(new[] { eventHandlerReady1, eventHandlerReady2 });

            using var conn = CreateRabbitMqConnection();
            var requests1 = new[]
            {
                "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten"
            };
            var rpcTasks = new List<Task>();
            using (var rpcClient = new RpcClient(conn))
            {
                Enumerable.Range(0, 10).ToList().ForEach(i =>
                {
                    rpcTasks.Add(rpcClient.CallAsync<TestRequest, TestResponse>(queueName1, new TestRequest { Prompt = requests1[i] }, 100));
                    rpcTasks.Add(rpcClient.CallAsync<FakeRequest, FakeResponse>(queueName2, new FakeRequest { Quantity = i }, 100));
                });
                Task.WaitAll(rpcTasks.ToArray());
            }

            Assert.AreEqual(10, rpcTasks.Where(t => t is Task<TestResponse>).Select(t => t as Task<TestResponse>).Count(t => t.Result.Reply.ToUpper() == t.Result.Reply));
            Assert.AreEqual(10, rpcTasks.Where(t => t is Task<FakeResponse>).Select(t => t as Task<FakeResponse>).Count());

            eventFinish1.Set();
            eventFinish2.Set();
            await Task.WhenAll(handlerTask1, handlerTask2);
        }

        /// <summary>
        /// Tests the ability of the client to survive cancellations, timeout errors, and request processing errors. 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task RpcClientSurvivalAfterErrorsTestAsync()
        {
            var queueName = Guid.NewGuid().ToString();
            var eventHandlerReady = new AutoResetEvent(false);
            var eventFinish = new AutoResetEvent(false);
            var errorMessage = "some error message";

            var handlerTask = Task.Run(() =>
            {
                using var conn = CreateRabbitMqConnection();
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(conn);
                rpcHandler.Start(queueName, (request) =>
                {
                    if (request.Prompt == "timeout")
                        Task.Delay(20000).Wait();
                    else if (request.Prompt == "error")
                        throw new ApplicationException(errorMessage);

                    return new TestResponse { Reply = "ok" };
                });
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();

            using (var conn = CreateRabbitMqConnection())
            {
                using var rpcClient = new RpcClient(conn);

                var response = await rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest());
                Assert.AreEqual("ok", response.Reply);
                Debug.WriteLine("First success request");

                try
                {
                    var cts = new CancellationTokenSource(5000);
                    response = await rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = "timeout" }, cts.Token);
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(OperationCanceledException));
                }

                response = await rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest(), 20);
                Assert.AreEqual("ok", response.Reply);
                Debug.WriteLine("Second success request");

                try
                {
                    response = await rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = "timeout" });
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(TimeoutException));
                }

                response = await rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest());
                Assert.AreEqual("ok", response.Reply);
                Debug.WriteLine("Third success request");

                try
                {
                    response = await rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = "error" });
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(RpcException));
                    Assert.AreEqual(errorMessage, ex.Message);
                }

                response = await rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest());
                Assert.AreEqual("ok", response.Reply);
                Debug.WriteLine("Last success request");

                eventFinish.Set();
                await handlerTask;
            }
        }
    }
}