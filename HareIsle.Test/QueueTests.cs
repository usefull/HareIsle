using HareIsle.EventArgs;
using HareIsle.Exceptions;
using HareIsle.Test.Assets;
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
            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDelete(TestQueueName, false, false);
        }

        [TestCleanup()]
        public void TestCleanup()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDelete(TestQueueName, false, false);
        }

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(MessageRoutingException))]
        public void EmittToNonExistentQueue_Test()
        {
            try
            {
                using var connection = Env.RabbitConnectionFactory.CreateConnection();

                using var emitter = new Emitter("1", connection);

                emitter.Enqueue(TestQueueName, new TestQueueMessage
                {
                    Text = "confirm"
                });
            }
            catch (Exception ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        [TestMethod]
        [Timeout(30000)]
        [ExpectedException(typeof(MessageNackException))]
        public void EmittToFullLimitQueue_Test()
        {
            try
            {
                using var connection = Env.RabbitConnectionFactory.CreateConnection();

                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(TestQueueName, true, false, false, new Dictionary<string, object>() { { "x-max-length", 1 }, { "x-overflow", "reject-publish" } });
                    channel.BasicPublish(string.Empty, TestQueueName, true, null, new byte[] { 1 });
                }

                using var emitter = new Emitter("1", connection);

                emitter.Enqueue(TestQueueName, new TestQueueMessage
                {
                    Text = "confirm"
                });
            }
            catch (Exception ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void Emitt_SuccessTest()
        {
            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            using var emitter = new Emitter("1", connection);

            emitter.DeclareQueue(TestQueueName, 2);

            emitter.Enqueue(TestQueueName, new TestQueueMessage
            {
                Text = "confirm"
            });
        }

        [TestMethod]
        [Timeout(30000)]
        public void InvalidMessageQueue_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var errEvent = new AutoResetEvent(false);

            var errors = new List<object>();
            var messages = new List<object>();

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using var emitter = new Emitter("1", connection);

            emitter.DeclareQueue(TestQueueName, 10);

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var handler = new QueueHandler<ImpRestrictObject>("2", conn, TestQueueName, 5, q => messages.Add(q));
                handler.OnError += (s, ea) =>
                {
                    errors.Add(ea);
                    errEvent.Set();
                };

                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            emitter.Enqueue(TestQueueName, new ImpRestrictObject
            {
                Value = -1
            });
            errEvent.WaitOne();
            endEvent.Set();

            handlerTask.Wait();

            Assert.AreEqual((uint)0, emitter.GetMessageCount(TestQueueName));
            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(0, messages.Count);

            var ea = (ErrorEventArgs<ImpRestrictObject, ImpRestrictObject>)errors[0];
            Assert.IsTrue(ea.Type == ErrorType.Validating);
        }

        [TestMethod]
        [Timeout(30000)]
        public void Queue_SuccessTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var msgEvent = new AutoResetEvent(false);

            var incomings = new List<object>();
            var handled = new List<object>();
            var errors = new List<object>();

            string? msg = null;

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using (var emitter = new Emitter("1", connection))
            {
                emitter.DeclareQueue(TestQueueName, 10);

                var handlerTask = Task.Run(() =>
                {
                    using var conn = Env.RabbitConnectionFactory.CreateConnection();
                    using var handler = new QueueHandler<TestQueueMessage>("1", conn, TestQueueName, 5, q =>
                    {
                        msg = q.Text;
                        msgEvent.Set();
                    });
                    handler.OnIncoming += (s, ea) => incomings.Add(ea);
                    handler.OnHandled += (s, ea) => handled.Add(ea);
                    handler.OnError += (s, ea) => errors.Add(ea);

                    readyEvent.Set();
                    endEvent.WaitOne();
                });

                readyEvent.WaitOne();

                emitter.Enqueue(TestQueueName, new TestQueueMessage
                {
                    Text = "confirm"
                });
                msgEvent.WaitOne();
                endEvent.Set();

                handlerTask.Wait();

                Assert.AreEqual((uint)0, emitter.GetMessageCount(TestQueueName));
            }

            Assert.IsTrue(msg == "confirm");
            Assert.AreEqual(errors.Count, 0);
            Assert.AreEqual(incomings.Count, 1);
            Assert.AreEqual(handled.Count, 1);
        }

        [TestMethod]
        [Timeout(30000)]
        public void MultiMessage_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            var msg = new ConcurrentBag<string?>();
            int counter = 100;

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using (var emitter = new Emitter("1", connection))
            {
                emitter.DeclareQueue(TestQueueName, 100);

                var handlerTask = Task.Run(() =>
                {
                    using var conn = Env.RabbitConnectionFactory.CreateConnection();

                    using var queue = new QueueHandler<TestQueueMessage>("1", conn, TestQueueName, 10, q =>
                    {
                        msg.Add(q.Text);
                    });

                    readyEvent.Set();

                    endEvent.WaitOne();
                });

                readyEvent.WaitOne();

                for (int i = 0; i < counter; i++)
                {
                    emitter.Enqueue(TestQueueName, new TestQueueMessage
                    {
                        Text = $"msg_{i}"
                    });
                }

                while (msg.Count < counter)
                    Task.Delay(1000).Wait();

                endEvent.Set();

                handlerTask.Wait();
            }

            Assert.IsTrue(msg.Count == counter);
        }

        [TestMethod]
        [Timeout(60000)]
        public void MultiHandler_Test()
        {
            var readyOneEvent = new AutoResetEvent(false);
            var endOneEvent = new AutoResetEvent(false);

            var readyTwoEvent = new AutoResetEvent(false);
            var endTwoEvent = new AutoResetEvent(false);

            int counter = 100;

            var msg1 = new ConcurrentBag<string?>();
            var msg2 = new ConcurrentBag<string?>();

            var random = new Random();

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using (var emitter = new Emitter("1", connection))
            {
                emitter.DeclareQueue(TestQueueName, 100);

                var handlerOneTask = Task.Run(async () =>
                {
                    using var conn = Env.RabbitConnectionFactory.CreateConnection();

                    using var queue = new QueueHandler<TestQueueMessage>("1", conn, TestQueueName, 10, q =>
                    {
                        Task.Delay(random.Next(100, 2000)).Wait();
                        msg1.Add(q.Text);
                    });

                    readyOneEvent.Set();

                    endOneEvent.WaitOne();
                });

                var handlerTwoTask = Task.Run(async () =>
                {
                    using var conn = Env.RabbitConnectionFactory.CreateConnection();

                    using var queue = new QueueHandler<TestQueueMessage>("1", conn, TestQueueName, 10, q =>
                    {
                        Task.Delay(random.Next(100, 2000)).Wait();
                        msg2.Add(q.Text);
                    });

                    readyTwoEvent.Set();

                    endTwoEvent.WaitOne();
                });

                readyOneEvent.WaitOne();
                readyTwoEvent.WaitOne();

                for (int i = 0; i < counter; i++)
                {
                    emitter.Enqueue(TestQueueName, new TestQueueMessage
                    {
                        Text = $"msg_{i}"
                    });
                }


                while (counter > msg1.Count + msg2.Count)
                    Task.Delay(1000).Wait();

                endOneEvent.Set();
                endTwoEvent.Set();

                Task.WaitAll(handlerOneTask, handlerTwoTask);
            }

            Assert.IsTrue(counter == msg1.Count + msg2.Count);
            Assert.IsTrue(!msg1.IsEmpty);
            Assert.IsTrue(!msg2.IsEmpty);
        }

        /// <summary>
        /// This test requires manually disabling/enabling the network.
        /// Run only in debug mode by commenting out the [Ignore] attribute
        /// and placing breakpoints at the places where the network is turned on / off
        /// (see comments into the test method body).
        /// </summary>
        [Ignore]
        [TestMethod]
        public void QueueAfterConnectionRecovery_SuccessTest()
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

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using (var emitter = new Emitter("1", connection))
            {
                emitter.DeclareQueue(TestQueueName, 10);

                var handlerTask = Task.Run(() =>
                {
                    using var conn = Env.RabbitConnectionFactory.CreateConnection();
                    using var handler = new QueueHandler<TestQueueMessage>("1", conn, TestQueueName, 5, q =>
                    {
                        msgEvent.Set();
                    });
                    handler.OnIncoming += (s, ea) => incomings.Add(ea);
                    handler.OnHandled += (s, ea) => handled.Add(ea);
                    handler.OnError += (s, ea) => errors.Add(ea);
                    handler.Stopped += (s, ev) => stoppedEvent.Set();

                    readyEvent.Set();

                    needToStartedSwitchOnEvent.WaitOne();
                    handler.Started += (s, ev) => startedEvent.Set();

                    endEvent.WaitOne();
                });

                readyEvent.WaitOne();

                emitter.Enqueue(TestQueueName, new TestQueueMessage
                {
                    Text = "confirm"
                });
                msgEvent.WaitOne();

                needToStartedSwitchOnEvent.Set();

                Task.Delay(5000).Wait();

                //here you need to manually TURN OFF the network
                ;
                stoppedEvent.WaitOne();

                try
                {
                    emitter.Enqueue(TestQueueName, new TestQueueMessage
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
                startedEvent.WaitOne();

                emitter.Enqueue(TestQueueName, new TestQueueMessage
                {
                    Text = "confirm"
                });
                msgEvent.WaitOne();

                endEvent.Set();

                handlerTask.Wait();
            }

            Assert.AreEqual(errors.Count, 0);
            Assert.AreEqual(incomings.Count, 2);
            Assert.AreEqual(handled.Count, 2);
        }
    }
}