using System.Collections.Concurrent;
using HareIsle.Exceptions;
using HareIsle.Test.Assets;
using Microsoft.VisualStudio.Threading;
using RabbitMQ.Client.Exceptions;

namespace HareIsle.Test
{
    [TestClass]
    public class BroadcastTests
    {
        [TestMethod]
        [Timeout(30000)]
        public async Task Broadcast_SuccessTest()
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

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();

            string? msg1 = null;
            string? msg2 = null;

            var handler1Task = Task.Run(async () =>
            {
                using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var broadcast = new BroadcastHandler<TestNotify>("1", conn, "3", notify =>
                {
                    msg1 = notify.Message;
                    msg1Event.Set();
                });
                broadcast.OnIncoming += async (s, ea) => await Task.Run(() => incomings.Add(ea));
                broadcast.OnHandled += async (s, ea) => await Task.Run(() => handled.Add(ea));
                broadcast.OnError += async (s, ea) => await Task.Run(() => errors.Add(ea));

                ready1Event.Set();
                await end1Event.ToTask();
            });

            var handler2Task = Task.Run(async () =>
            {
                using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var broadcast = new BroadcastHandler<TestNotify>("2", conn, "3", notify =>
                {
                    msg2 = notify.Message;
                    msg2Event.Set();
                });
                broadcast.OnIncoming += async (s, ea) => await Task.Run(() => incomings.Add(ea));
                broadcast.OnHandled += async (s, ea) => await Task.Run(() => handled.Add(ea));
                broadcast.OnError += async (s, ea) => await Task.Run(() => errors.Add(ea));

                ready2Event.Set();
                await end2Event.ToTask();
            });

            await Task.WhenAll(ready1Event.ToTask(), ready2Event.ToTask());

            using (var emitter = new Emitter("3", connection))
            {
                await emitter.BroadcastAsync(new TestNotify
                {
                    Message = "1"
                });

                await Task.WhenAll(msg1Event.ToTask(), msg2Event.ToTask());

                end1Event.Set();
                end2Event.Set();
            }

            await Task.WhenAll(handler1Task, handler2Task);

            Assert.AreEqual("1", msg1);
            Assert.AreEqual("1", msg2);
            Assert.AreEqual(2, incomings.Count);
            Assert.AreEqual(2, handled.Count);
            Assert.IsTrue(errors.IsEmpty);
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task InterriptedBroadcast_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            var notifications = new List<TestNotify>();

            var handlerTask = Task.Run(async () =>
            {
                var rnd = new Random();
                using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var broadcast = new BroadcastHandler<TestNotify>("1", conn, "2", notify =>
                {
                    if (notifications.Count > 10)
                        endEvent.Set();

                    Task.Delay(rnd.Next(500, 2000)).Wait();
                    notifications.Add(notify);
                });

                readyEvent.Set();
                await endEvent.ToTask();
            });

            await readyEvent.ToTask();

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();
            using var emitter = new Emitter("2", connection);

            while (!handlerTask.IsCompleted)
            {
                await Task.Delay(100);
                await emitter.BroadcastAsync(new TestNotify
                {
                    Message = "notify"
                });
            }

            Assert.IsTrue(notifications.Count > 10);
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task SkipBroadcast_Test()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();

            var notifications = new List<TestNotify>();

            var handlerTask = Task.Run(async () =>
            {
                using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var broadcast = new BroadcastHandler<TestNotify>("1", conn, "2", notify =>
                {
                    notifications.Add(notify);
                });

                readyEvent.Set();
                await endEvent.ToTask();
            });

            await readyEvent.ToTask();

            using (var emitter = new Emitter("2", connection))
            {
                for (int i = 0; i < 10; i++)
                    await emitter.BroadcastAsync(new TestNotifyToSkip
                    {
                        Value = i
                    });

                await emitter.BroadcastAsync(new TestNotify
                {
                    Message = "notify"
                });

                for (int i = 0; i < 10; i++)
                    await emitter.BroadcastAsync(new TestNotifyToSkip
                    {
                        Value = i
                    });

                await emitter.BroadcastAsync(new TestNotify
                {
                    Message = "notify"
                });

                while (notifications.Count < 2)
                    await Task.Delay(1000);

                endEvent.Set();
            }

            await handlerTask;

            Assert.AreEqual(2, notifications.Count);
        }

        /// <summary>
        /// This test requires manually disabling/enabling the network.
        /// Run only in debug mode by commenting out the [Ignore] attribute
        /// and placing breakpoints at the places where the network is turned on / off
        /// (see comments into the test method body).
        /// </summary>
        [Ignore]
        [TestMethod]
        public async Task BroadcastAfterConnectionRecovery_SuccessTest()
        {
            var readyEvent = new AutoResetEvent(false);
            var endEvent = new AutoResetEvent(false);
            var stoppedEvent = new AutoResetEvent(false);
            var startedEvent = new AutoResetEvent(false);
            var needToStartedSwitchOnEvent = new AutoResetEvent(false);

            var notifications = new List<TestNotify>();

            var handlerTask = Task.Run(async () =>
            {
                var rnd = new Random();
                using var conn = await Env.RabbitConnectionFactory.CreateConnectionAsync();
                using var handler = new BroadcastHandler<TestNotify>("1", conn, "2", notify =>
                {
                    notifications.Add(notify);
                });
                handler.Stopped += async (_, _) => await Task.Run(() => stoppedEvent.Set());

                readyEvent.Set();
                await needToStartedSwitchOnEvent.ToTask();
                handler.Started += async (_, _) => await Task.Run(() => startedEvent.Set());

                await endEvent.ToTask();
            });

            await readyEvent.ToTask();

            using var connection = await Env.RabbitConnectionFactory.CreateConnectionAsync();
            using var emitter = new Emitter("2", connection);

            await emitter.BroadcastAsync(new TestNotify
            {
                Message = "notify"
            });

            while (!notifications.Any())
                await Task.Delay(1000);

            needToStartedSwitchOnEvent.Set();

            //here you need to manually TURN OFF the network
            ;
            await stoppedEvent.ToTask();

            try
            {
                await emitter.BroadcastAsync(new TestNotify
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
            await startedEvent.ToTask();

            await emitter.BroadcastAsync(new TestNotify
            {
                Message = "notify"
            });

            while (notifications.Count < 2)
                await Task.Delay(1000);

            Assert.AreEqual(2, notifications.Count);
        }
    }
}