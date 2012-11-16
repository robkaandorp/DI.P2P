using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DI.P2P;
using System.Threading;
using DI.P2P.Entities;

namespace DI.P2P.Tests
{
    [TestClass]
    public class ModuleTests
    {
        [TestInitialize]
        public void Initialize()
        {
            log4net.LogManager.Shutdown();
            log4net.Config.BasicConfigurator.Configure();
        }

        [TestMethod]
        public void StartModuleTest()
        {
            var module = new Module(Guid.NewGuid());
            module.Configure();

            Assert.IsTrue(!module.IsRunning);

            module.Start();

            Assert.IsTrue(module.IsRunning);

            module.Stop();

            Assert.IsTrue(!module.IsRunning);
        }

        [TestMethod]
        public void TestTwoModulesTest()
        {
            var id1 = Guid.NewGuid();
            var module1 = new Module(id1);
            module1.Configure();

            var id2 = Guid.NewGuid();
            var module2 = new Module(id2, 11112);
            module2.Configure();

            module1.Start();
            module2.Start();

            Assert.IsTrue(module1.IsRunning);
            Assert.IsTrue(module2.IsRunning);

            var client1 = module1.Find<ClientInterface>();
            var client2 = module2.Find<ClientInterface>();

            client1.Connect("localhost", 11112);

            AutoResetEvent ack1Event = new AutoResetEvent(false);
            client1.AckReceived += (sender, args) =>
                {
                    ack1Event.Set();
                };

            AutoResetEvent ack2Event = new AutoResetEvent(false);
            client2.AckReceived += (sender, args) =>
                {
                    ack2Event.Set();
                };

            client1.Ping(id2);
            client2.Ping(id1);  // callback

            Assert.IsTrue(ack1Event.WaitOne(TimeSpan.FromMinutes(1)));
            Assert.IsTrue(ack2Event.WaitOne(TimeSpan.FromMinutes(1)));

            module1.Stop();
            module2.Stop();
        }

        [TestMethod]
        public void TestThreeModulesTest()
        {
            var id1 = Guid.NewGuid();
            var module1 = new Module(id1);
            module1.Configure();

            var id2 = Guid.NewGuid();
            var module2 = new Module(id2, 11112);
            module2.Configure();

            var id3 = Guid.NewGuid();
            var module3 = new Module(id3, 11113);
            module3.Configure();

            module1.Start();
            module2.Start();
            module3.Start();

            Assert.IsTrue(module1.IsRunning);
            Assert.IsTrue(module2.IsRunning);
            Assert.IsTrue(module3.IsRunning);

            var client1 = module1.Find<ClientInterface>();
            var client2 = module2.Find<ClientInterface>();
            var client3 = module3.Find<ClientInterface>();

            client1.Connect("localhost", 11112);
            client3.Connect("localhost", 11112);

            AutoResetEvent msg1Event = new AutoResetEvent(false);
            client1.MessageReceived += (sender, args) =>
            {
                msg1Event.Set();
            };

            AutoResetEvent msg2Event = new AutoResetEvent(false);
            client2.MessageReceived += (sender, args) =>
            {
                msg2Event.Set();
            };

            AutoResetEvent msg3Event = new AutoResetEvent(false);
            client3.MessageReceived += (sender, args) =>
            {
                msg3Event.Set();
            };


            // send message from node1 to node3, this will be sent via node2
            client1.Send(new Message()
                {
                    From = module1.GetMyNode(),
                    To = id3,
                });

            Assert.IsFalse(msg2Event.WaitOne(TimeSpan.FromSeconds(2)));
            Assert.IsTrue(msg3Event.WaitOne(TimeSpan.FromSeconds(10)));


            // broadcast message from node3, this will be received by both node2 and node1
            client3.Send(new Message()
                {
                    From = module3.GetMyNode(),
                    To = null,
                });

            Assert.IsTrue(msg2Event.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.IsTrue(msg1Event.WaitOne(TimeSpan.FromSeconds(10)));


            module1.Stop();
            module2.Stop();
            module3.Stop();
        }
    }
}
