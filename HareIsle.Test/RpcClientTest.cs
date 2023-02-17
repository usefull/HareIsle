using RabbitMQ.Client.Exceptions;
using System.Diagnostics;
using static HareIsle.Test.Equipment;

namespace HareIsle.Test
{
    /// <summary>
    /// <see cref="RpcClient"/> test cases.
    /// </summary>
    [TestClass]
    public class RpcClientTest
    {
        /// <summary>
        /// Tests for a timeout on a RPC request.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task RpcClientTimeoutTestAsync()
        {
            var timeoutInSeconds = 20;
            var queueName = Guid.NewGuid().ToString();
            using var conn = CreateRabbitMqConnection();
            using var rpcClient = new RpcClient(conn);
            var sw = Stopwatch.StartNew();
            try
            {
                await rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest { Prompt = "some text" }, timeoutInSeconds);
            }
            catch
            {
                throw;
            }
            finally
            {
                sw.Stop();
                Assert.IsTrue(sw.ElapsedMilliseconds >= timeoutInSeconds * 1000);
            }
        }

        /// <summary>
        /// Tests exception in the case of RPC client creation on closed connection.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(AlreadyClosedException))]
        public void RpcClientCreationOnClosedConnectionTest()
        {
            using var clientConnection = CreateRabbitMqConnection();
            clientConnection.Close();
            new RpcClient(clientConnection);
        }

        /// <summary>
        /// Tests request cancellation.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(AlreadyClosedException))]
        public async Task RpcRequestOnClosedConnectionAsync()
        {
            var queueName = Guid.NewGuid().ToString();
            using var clientConnection = CreateRabbitMqConnection();
            using var rpcClient = new RpcClient(clientConnection);
            clientConnection.Close();
            _ = await rpcClient.CallAsync<TestRequest, TestResponse>(queueName, new TestRequest());
        }
    }
}