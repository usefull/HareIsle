using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HareIsle.Test
{
    [TestClass]
    public class Init
    {
        public static IConnection? Connection { get; private set; }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            var factory = new ConnectionFactory { Uri = new Uri("amqps://wvscvfrx:ZtzifEsBvWnFNb4PVNDN8X2VN5GFi4Wh@hawk.rmq.cloudamqp.com/wvscvfrx") };
            Connection = factory.CreateConnection();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            if (Connection != null && !Connection.IsOpen)
            {
                Connection.Close();
                Connection.Dispose();
            }
        }
    }
}
