using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HareIsle.Test.Equipment;

namespace HareIsle.Test
{
    [TestClass]
    public class RpcServerTest
    {
        [TestMethod]
        public void www()
        {
            
            new RpcServer(CreateRabbitMqConnection())
                .Method<int, string>("111", (i) => i.ToString())
                .Test("111");
        }
    }
}