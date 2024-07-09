using System.Collections.Concurrent;
using HareIsle.Exceptions;
using HareIsle.Test.Assets;
using RabbitMQ.Client.Exceptions;

namespace HareIsle.Test
{
    [TestClass]
    public class BroadcastTests
    {
        [TestMethod]
        [Timeout(5000)]
        public void Broadcast_SuccessTest()
        {
            var ready1Event = new AutoResetEvent(false);
            var msg1Event = new AutoResetEvent(false);

            var ready2Event = new AutoResetEvent(false);
            var msg2Event = new AutoResetEvent(false);

            var end1Event = new AutoResetEvent(false);
            var end2Event = new AutoResetEvent(false);

            var incomings = new ConcurrentBag<object>();
            var handled = new ConcurrentBag<object>();
            var errors = new ConcurrentBag<object>();

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            string? msg1 = null;
            string? msg2 = null;

            var handler1Task = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var broadcast = new BroadcastHandler<TestNotify>("1", conn, "3", notify =>
                {
                    msg1 = notify.Message;
                    msg1Event.Set();
                });
                broadcast.OnIncoming += (s, ea) => incomings.Add(ea);
                broadcast.OnHandled += (s, ea) => handled.Add(ea);
                broadcast.OnError += (s, ea) => errors.Add(ea);

                ready1Event.Set();
                end1Event.WaitOne();
            });

            var handler2Task = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var broadcast = new BroadcastHandler<TestNotify>("2", conn, "3", notify =>
                {
                    msg2 = notify.Message;
                    msg2Event.Set();
                });
                broadcast.OnIncoming += (s, ea) => incomings.Add(ea);
                broadcast.OnHandled += (s, ea) => handled.Add(ea);
                broadcast.OnError += (s, ea) => errors.Add(ea);

                ready2Event.Set();
                end2Event.WaitOne();
            });

            ready1Event.WaitOne();
            ready2Event.WaitOne();

            using (var emitter = new Emitter("3", connection))
            {
                emitter.Broadcast(new TestNotify
                {
                    Message = "1"
                });

                msg1Event.WaitOne();
                msg2Event.WaitOne();

                end1Event.Set();
                end2Event.Set();
            }

            handler1Task.Wait();
            handler2Task.Wait();

            Assert.AreEqual(msg1, "1");
            Assert.AreEqual(msg2, "1");
            Assert.IsTrue(incomings.Count == 2);
            Assert.IsTrue(handled.Count == 2);
            Assert.IsTrue(errors.IsEmpty);
        }

        [TestMethod]
        [Timeout(15000)]
        public void InterriptedBroadcast_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            var notifications = new List<TestNotify>();

            var handlerTask = Task.Run(() =>
            {
                var rnd = new Random();
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var broadcast = new BroadcastHandler<TestNotify>("1", conn, "2", notify =>
                {
                    if (notifications.Count > 10)
                        endEvent.Set();

                    Task.Delay(rnd.Next(500, 2000)).Wait();
                    notifications.Add(notify);
                });

                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using var emitter = new Emitter("2", connection);

            while (!handlerTask.IsCompleted)
            {
                Task.Delay(100).Wait();
                emitter.Broadcast(new TestNotify
                {
                    Message = "notify"
                });
            }

            Assert.IsTrue(notifications.Count > 10);
        }

        [TestMethod]
        [Timeout(15000)]
        public void SkipBroadcast_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = Env.RabbitConnectionFactory.CreateConnection();

            var notifications = new List<TestNotify>();

            var handlerTask = Task.Run(() =>
            {
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var broadcast = new BroadcastHandler<TestNotify>("1", conn, "2", notify =>
                {
                    notifications.Add(notify);
                });

                readyEvent.Set();
                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using (var emitter = new Emitter("2", connection))
            {
                for (int i = 0; i < 10; i++)
                    emitter.Broadcast(new TestNotifyToSkip
                    {
                        Value = i
                    });

                emitter.Broadcast(new TestNotify
                {
                    Message = "notify"
                });

                for (int i = 0; i < 10; i++)
                    emitter.Broadcast(new TestNotifyToSkip
                    {
                        Value = i
                    });

                emitter.Broadcast(new TestNotify
                {
                    Message = "notify"
                });

                while (notifications.Count < 2)
                    Task.Delay(1000).Wait();

                endEvent.Set();
            }

            handlerTask.Wait();

            Assert.IsTrue(notifications.Count == 2);
        }

        /// <summary>
        /// This test requires manually disabling/enabling the network.
        /// Run only in debug mode by commenting out the [Ignore] attribute
        /// and placing breakpoints at the places where the network is turned on / off
        /// (see comments into the test method body).
        /// </summary>
        [Ignore]
        [TestMethod]
        public void SuccessBroadcastAfterConnectionRecoveryTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var stoppedEvent = new AutoResetEvent(false);
            var startedEvent = new AutoResetEvent(false);
            var needToStartedSwitchOnEvent = new AutoResetEvent(false);

            var notifications = new List<TestNotify>();

            var handlerTask = Task.Run(() =>
            {
                var rnd = new Random();
                using var conn = Env.RabbitConnectionFactory.CreateConnection();
                using var handler = new BroadcastHandler<TestNotify>("1", conn, "2", notify =>
                {
                    notifications.Add(notify);
                });
                handler.Stopped += (_, _) => stoppedEvent.Set();

                readyEvent.Set();
                needToStartedSwitchOnEvent.WaitOne();
                handler.Started += (_, _) => startedEvent.Set();

                endEvent.WaitOne();
            });

            readyEvent.WaitOne();

            using var connection = Env.RabbitConnectionFactory.CreateConnection();
            using var emitter = new Emitter("2", connection);

            emitter.Broadcast(new TestNotify
            {
                Message = "notify"
            });

            while (!notifications.Any())
                Task.Delay(1000).Wait();

            needToStartedSwitchOnEvent.Set();

            //here you need to manually TURN OFF the network
            ;
            stoppedEvent.WaitOne();

            try
            {
                emitter.Broadcast(new TestNotify
                {
                    Message = "lost notify"
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

            emitter.Broadcast(new TestNotify
            {
                Message = "notify"
            });

            while (notifications.Count < 2)
                Task.Delay(1000).Wait();

            Assert.IsTrue(notifications.Count == 2);
        }
    }
}