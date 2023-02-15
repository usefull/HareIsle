using HareIsle.Exceptions;
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

            var handlerTask = Task.Run(() =>
            {
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(Connection!);
                rpcHandler.Start(queueName, (request) => new TestResponse { Reply = request.Prompt!.ToUpper() });
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();

            var rpcClient = new RpcClient(Equipment.Connection!);
            var requestTask = rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = prompt });
            requestTask.Wait();
            var response = requestTask.Result;

            eventFinish.Set();

            handlerTask.Wait();

            Assert.AreEqual(response.Reply, prompt.ToUpper());
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
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(Connection!);
                rpcHandler.Start(queueName, (request) => new TestResponse { Reply = request.Prompt!.ToUpper() }, 5);
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();

            var clientTask1 = Task.Run(() =>
            {                
                var rpcClient = new RpcClient(Connection!) { Timeout = 20 };
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
                var rpcClient = new RpcClient(Connection!) { Timeout = 20 };
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
                var rpcClient = new RpcClient(Connection!) { Timeout = 20 };
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

            var handlerTask = Task.Run(() =>
            {
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(Connection!);
                rpcHandler.Start(queueName, (request) => throw new ApplicationException(errorMessage));
                eventHandlerReady.Set();
                eventFinish.WaitOne();
            });

            eventHandlerReady.WaitOne();

            var rpcClient = new RpcClient(Connection!);

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
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(Connection!);
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
            var rpcClient = new RpcClient(Connection!);

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
    }
}