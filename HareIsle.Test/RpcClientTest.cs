using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HareIsle.Test.Equipment;

namespace HareIsle.Test
{
    [TestClass]
    public class RpcClientTest
    {
        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task RpcClientTimeoutTestAsync()
        {
            var timeoutInSeconds = 20;
            var queueName = Guid.NewGuid().ToString();
            var rpcClient = new RpcClient(Equipment.Connection!);
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