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
        public static IConnection? Connection { get; private set; }

        /// <summary>
        /// Performs necessary initialization actions before executing tests in this assembly.
        /// </summary>
        /// <param name="context">Test context.</param>
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            var factory = new ConnectionFactory { Uri = new Uri("amqps://wvscvfrx:ZtzifEsBvWnFNb4PVNDN8X2VN5GFi4Wh@hawk.rmq.cloudamqp.com/wvscvfrx") };
            Connection = factory.CreateConnection();
        }

        /// <summary>
        /// Performs necessary finishing actions after the tests in the given assembly are completed.
        /// </summary>
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            if (Connection != null && !Connection.IsOpen)
            {
                Connection.Close();
                Connection.Dispose();
            }
        }

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
    }
}