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
            var rpcClient = new RpcClient(CreateRabbitMqConnection());
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
    }
}