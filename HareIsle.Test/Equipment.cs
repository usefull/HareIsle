using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HareIsle.Test
{
    [TestClass]
    public class Equipment
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

        internal class TestRequest : IValidatableObject
        {
            public string? Prompt { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return Enumerable.Empty<ValidationResult>();
            }
        }

        internal class TestResponse : IValidatableObject
        {
            public string? Reply { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return Enumerable.Empty<ValidationResult>();
            }
        }
    }
}
