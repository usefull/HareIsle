﻿using static HareIsle.Test.Equipment;

namespace HareIsle.Test
{
    /// <summary>
    /// <see cref="RpcClient"/> and <see cref="RpcHandler{TRequest, TResponse}"/> integration test cases.
    /// </summary>
    [TestClass]
    public class RpcIntegrationTest
    {
        /// <summary>
        /// Tests successful RPC request.
        /// </summary>
        [TestMethod]
        public void RpcIntegrationSuccessTest()
        {
            var prompt = "prompt";
            var queueName = Guid.NewGuid().ToString();
            var eventHandlerReady = new AutoResetEvent(false);
            var eventFinish = new AutoResetEvent(false);

            var handlerTask = Task.Run(() =>
            {
                using var rpcHandler = new RpcHandler<TestRequest, TestResponse>(Equipment.Connection!);
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
    }
}