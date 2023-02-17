using RabbitMQ.Client;
using System.ComponentModel.DataAnnotations;

namespace HareIsle.Test
{
    /// <summary>
    /// The set of auxiliary tools necessary for testing.
    /// </summary>
    [TestClass]
    public class Equipment
    {
        /// <summary>
        /// RabbitMQ connection object to use in the testing process.
        /// </summary>
        public static IConnection CreateRabbitMqConnection()
        {
            _rabbitMqConnectionFactory ??= new ConnectionFactory { Uri = new Uri(_rabbitMqUrl), AutomaticRecoveryEnabled = false };
            var connection = _rabbitMqConnectionFactory.CreateConnection();
            return connection;
        }

        /// <summary>
        /// Performs necessary initialization actions before executing tests in this assembly.
        /// </summary>
        /// <param name="context">Test context.</param>
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {            
        }

        /// <summary>
        /// Performs necessary finishing actions after the tests in the given assembly are completed.
        /// </summary>
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
        }

        private static ConnectionFactory? _rabbitMqConnectionFactory;
        private static readonly string _rabbitMqUrl = "amqps://wvscvfrx:ZtzifEsBvWnFNb4PVNDN8X2VN5GFi4Wh@hawk.rmq.cloudamqp.com/wvscvfrx";

        /// <summary>
        /// Some test request.
        /// </summary>
        internal class TestRequest : IValidatableObject
        {
            public string? Prompt { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return Enumerable.Empty<ValidationResult>();
            }
        }

        /// <summary>
        /// Some test response.
        /// </summary>
        internal class TestResponse : IValidatableObject
        {
            public string? Reply { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return Enumerable.Empty<ValidationResult>();
            }
        }

        /// <summary>
        /// Some test request.
        /// </summary>
        internal class FakeRequest : IValidatableObject
        {
            public int Quantity;

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return Enumerable.Empty<ValidationResult>();
            }
        }

        /// <summary>
        /// Some test response.
        /// </summary>
        internal class FakeResponse : IValidatableObject
        {
            public string? Total { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return Enumerable.Empty<ValidationResult>();
            }
        }
    }
}