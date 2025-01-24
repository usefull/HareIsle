using HareIsle.EventArgs;
using HareIsle.Exceptions;
using HareIsle.Test.Assets;
using Microsoft.VisualStudio.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Collections.Concurrent;

namespace HareIsle.Test
{
    [TestClass]
    public class QueueTests
    {
        static readonly string TestQueueName = "queue1";

        [TestInitialize()]
        public void TestInitialize()
        {
            var jtf = new JoinableTaskFactory(new JoinableTaskContext());
            jtf.Run(async () =>
            {
                using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();
                await channel.QueueDeleteAsync(TestQueueName, false, false);
            });
        }

        [TestCleanup()]
        public void TestCleanup()
        {
            var jtf = new JoinableTaskFactory(new JoinableTaskContext());
            jtf.Run(async () =>
            {
                using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();
                await channel.QueueDeleteAsync(TestQueueName, false, false);
            });
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task EmittToNonExistentQueue_Test()
        {
            await Assert.ThrowsExceptionAsync<PublishException>(async () =>
            {
                try
                {
                    using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();

                    using var emitter = new Emitter("1", connection);

                    await emitter.EnqueueAsync(TestQueueName, new TestQueueMessage
                    {
                        Text = "confirm"
                    });
                }
                catch (Exception ex)
                {
                    throw ex.InnerException ?? ex;
                }
            });
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task EmittToFullLimitQueue_Test()
        {
            await Assert.ThrowsExceptionAsync<PublishException>(async () =>
            {
                try
                {
                    using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();

                    using (var channel = await connection.CreateChannelAsync())
                    {
                        await channel.QueueDeclareAsync(TestQueueName, true, false, false, new Dictionary<string, object?>() { { "x-max-length", 1 }, { "x-overflow", "reject-publish" } });
                        await channel.BasicPublishAsync(string.Empty, TestQueueName, new byte[] { 1 }, default);
                    }

                    using var emitter = new Emitter("1", connection);

                    await emitter.EnqueueAsync(TestQueueName, new TestQueueMessage
                    {
                        Text = "confirm"
                    });
                }
                catch (Exception ex)
                {
                    throw ex.InnerException ?? ex;
                }
            });
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task Emitt_SuccessTest()
        {
            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();

            using var emitter = new Emitter("1", connection);

            await emitter.DeclareQueueAsync(TestQueueName, 2);

            await emitter.EnqueueAsync(TestQueueName, new TestQueueMessage
            {
                Text = "confirm"
            });
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task InvalidMessageQueue_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var errEvent = new AutoResetEvent(false);

            var errors = new List<object>();
            var messages = new List<object>();

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();
            using var emitter = new Emitter("1", connection);

            await emitter.DeclareQueueAsync(TestQueueName, 10);

            var handlerTask = Task.Run(async () =>
            {
                using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var handler = new QueueHandler<ImpRestrictObject>("2", conn, TestQueueName, 5, q => messages.Add(q));
                handler.OnError += async (s, ea) => await Task.Run(() =>
                {
                    errors.Add(ea);
                    errEvent.Set();
                });

                readyEvent.Set();
                await endEvent.ToTask();
            });

            await readyEvent.ToTask();

            await emitter.EnqueueAsync(TestQueueName, new ImpRestrictObject
            {
                Value = -1
            });
            await errEvent.ToTask();
            endEvent.Set();

            await handlerTask;

            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(0, messages.Count);

            var ea = (ErrorEventArgs<ImpRestrictObject, ImpRestrictObject>)errors[0];
            Assert.AreEqual(ErrorType.Validating, ea.Type);
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task Queue_SuccessTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var msgEvent = new AutoResetEvent(false);

            var incomings = new List<object>();
            var handled = new List<object>();
            var errors = new List<object>();

            string? msg = null;

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();
            using (var emitter = new Emitter("1", connection))
            {
                await emitter.DeclareQueueAsync(TestQueueName, 10);

                var handlerTask = Task.Run(async () =>
                {
                    using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                    using var handler = new QueueHandler<TestQueueMessage>("1", conn, TestQueueName, 5, q =>
                    {
                        msg = q.Text;
                        msgEvent.Set();
                    });
                    handler.OnIncoming += async (s, ea) => await Task.Run(() => incomings.Add(ea));
                    handler.OnHandled += async (s, ea) => await Task.Run(() => handled.Add(ea));
                    handler.OnError += async (s, ea) => await Task.Run(() => errors.Add(ea));

                    readyEvent.Set();
                    await endEvent.ToTask();
                });

                await readyEvent.ToTask();

                await emitter.EnqueueAsync(TestQueueName, new TestQueueMessage
                {
                    Text = "confirm"
                });
                await msgEvent.ToTask();
                endEvent.Set();

                await handlerTask;                
            }

            Assert.AreEqual("confirm", msg);
            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(1, incomings.Count);
            Assert.AreEqual(1, handled.Count);
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task MultiMessage_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            var msg = new ConcurrentBag<string?>();
            int counter = 100;

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();
            using (var emitter = new Emitter("1", connection))
            {
                await emitter.DeclareQueueAsync(TestQueueName, 100);

                var handlerTask = Task.Run(async () =>
                {
                    using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();

                    using var queue = new QueueHandler<TestQueueMessage>("1", conn, TestQueueName, 10, q =>
                    {
                        msg.Add(q.Text);
                    });

                    readyEvent.Set();

                    await endEvent.ToTask();
                });

                await readyEvent.ToTask();

                for (int i = 0; i < counter; i++)
                {
                    await emitter.EnqueueAsync(TestQueueName, new TestQueueMessage
                    {
                        Text = $"msg_{i}"
                    });
                }

                while (msg.Count < counter)
                    await Task.Delay(1000);

                endEvent.Set();

                await handlerTask;
            }

            Assert.AreEqual(counter, msg.Count);
        }

        [TestMethod]
        [Timeout(60000)]
        public async Task MultiHandler_Test()
        {
            var readyOneEvent = new AutoResetEvent(false);
            var endOneEvent = new AutoResetEvent(false);

            var readyTwoEvent = new AutoResetEvent(false);
            var endTwoEvent = new AutoResetEvent(false);

            int counter = 100;

            var msg1 = new ConcurrentBag<string?>();
            var msg2 = new ConcurrentBag<string?>();

            var random = new Random();

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();
            using (var emitter = new Emitter("1", connection))
            {
                await emitter.DeclareQueueAsync(TestQueueName, 100);

                var handlerOneTask = Task.Run(async () =>
                {
                    using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();

                    using var queue = new QueueHandler<TestQueueMessage>("1", conn, TestQueueName, 10, q =>
                    {
                        Task.Delay(random.Next(100, 2000)).Wait();
                        msg1.Add(q.Text);
                    });

                    readyOneEvent.Set();

                    await endOneEvent.ToTask();
                });

                var handlerTwoTask = Task.Run(async () =>
                {
                    using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();

                    using var queue = new QueueHandler<TestQueueMessage>("1", conn, TestQueueName, 10, q =>
                    {
                        Task.Delay(random.Next(100, 2000)).Wait();
                        msg2.Add(q.Text);
                    });

                    readyTwoEvent.Set();

                    await endTwoEvent.ToTask();
                });

                await Task.WhenAll(readyOneEvent.ToTask(),readyTwoEvent.ToTask());

                for (int i = 0; i < counter; i++)
                {
                    await emitter.EnqueueAsync(TestQueueName, new TestQueueMessage
                    {
                        Text = $"msg_{i}"
                    });
                }


                while (counter > msg1.Count + msg2.Count)
                    await Task.Delay(1000);

                endOneEvent.Set();
                endTwoEvent.Set();

                await Task.WhenAll(handlerOneTask, handlerTwoTask);
            }

            Assert.AreEqual(msg1.Count + msg2.Count, counter);
            Assert.IsFalse(msg1.IsEmpty);
            Assert.IsFalse(msg2.IsEmpty);
        }

        /// <summary>
        /// This test requires manually disabling/enabling the network.
        /// Run only in debug mode by commenting out the [Ignore] attribute
        /// and placing breakpoints at the places where the network is turned on / off
        /// (see comments into the test method body).
        /// </summary>
        [Ignore]
        [TestMethod]
        public async Task QueueAfterConnectionRecovery_SuccessTest()
        {
            var endEvent = new AutoResetEvent(false);
            var msgEvent = new AutoResetEvent(false);
            var stoppedEvent = new AutoResetEvent(false);
            var startedEvent = new AutoResetEvent(false);
            var readyEvent = new AutoResetEvent(false);
            var needToStartedSwitchOnEvent = new AutoResetEvent(false);

            var incomings = new List<object>();
            var handled = new List<object>();
            var errors = new List<object>();

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();
            using (var emitter = new Emitter("1", connection))
            {
                await emitter.DeclareQueueAsync(TestQueueName, 10);

                var handlerTask = Task.Run(async () =>
                {
                    using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                    using var handler = new QueueHandler<TestQueueMessage>("1", conn, TestQueueName, 5, q =>
                    {
                        msgEvent.Set();
                    });
                    handler.OnIncoming += async (s, ea) => await Task.Run(() => incomings.Add(ea));
                    handler.OnHandled += async (s, ea) => await Task.Run(() => handled.Add(ea));
                    handler.OnError += async (s, ea) => await Task.Run(() => errors.Add(ea));
                    handler.Stopped += async (s, ev) => await Task.Run(() => stoppedEvent.Set());

                    readyEvent.Set();

                    await needToStartedSwitchOnEvent.ToTask();
                    handler.Started += async (s, ev) => await Task.Run(() => startedEvent.Set());

                    await endEvent.ToTask();
                });

                await readyEvent.ToTask();

                await emitter.EnqueueAsync(TestQueueName, new TestQueueMessage
                {
                    Text = "confirm"
                });
                await msgEvent.ToTask();

                needToStartedSwitchOnEvent.Set();

                await Task.Delay(5000);

                //here you need to manually TURN OFF the network
                ;
                await stoppedEvent.ToTask();

                try
                {
                    await emitter.EnqueueAsync(TestQueueName, new TestQueueMessage
                    {
                        Text = "lost confirm"
                    });
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(SendingException));
                    Assert.IsInstanceOfType(ex.InnerException, typeof(AlreadyClosedException));
                }

                //here you need to manually TURN ON the network
                ;
                await startedEvent.ToTask();

                await emitter.EnqueueAsync(TestQueueName, new TestQueueMessage
                {
                    Text = "confirm"
                });
                await msgEvent.ToTask();

                endEvent.Set();

                await handlerTask;
            }

            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(2, incomings.Count);
            Assert.AreEqual(2, handled.Count);
        }
    }
}