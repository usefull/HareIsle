using RabbitMQ.Client;

namespace HareIsle.Test.Assets
{
    public static class Env
    {
        private static readonly Uri rabbitUri = new("amqp://user:user@shost152:5672/");

        private static readonly ConnectionFactory rabbitConnectionFactory = new()
        {
            Uri = rabbitUri,
            ConsumerDispatchConcurrency = 10,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            RequestedHeartbeat = TimeSpan.FromSeconds(10)
        };

        public static ConnectionFactory RabbitConnectionFactory => rabbitConnectionFactory;
    }
}