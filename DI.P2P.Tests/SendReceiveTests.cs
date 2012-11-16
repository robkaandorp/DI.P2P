using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace DI.P2P.Tests
{
    [TestClass]
    public class SendReceiveTests
    {
        protected log4net.ILog Logger;

        [TestInitialize]
        public void Initialize()
        {
            log4net.LogManager.Shutdown();
            log4net.Config.BasicConfigurator.Configure();
            Logger = log4net.LogManager.GetLogger(GetType());
        }

        [TestMethod]
        public void TwoNodesTest()
        {
            var module1 = new Module(Guid.NewGuid());
            module1.Configure();
            module1.Start();

            var module2 = new Module(Guid.NewGuid(), 11112);
            module2.Configure();
            module2.Start();

            Assert.IsTrue(module1.IsRunning);
            Assert.IsTrue(module2.IsRunning);


            var client1 = module1.Find<ClientInterface>();
            var client2 = module2.Find<ClientInterface>();

            client2.Connect("localhost", 11111);


            var msgEvent = new AutoResetEvent(false);
            client1.MessageReceived += (sender, args) =>
                {
                    Logger.Info("Client1 message received.");
                    msgEvent.Set();
                };

            client2.MessageReceived += (sender, args) =>
                {
                    Logger.Info("Client2 message received.");
                    Assert.IsNotNull(args.Data.Path);
                    Assert.IsTrue(args.Data.Path.Count == 2);
                    Assert.IsTrue(args.Data.Path[0] == module1.Id);
                    Assert.IsTrue(args.Data.Path[1] == module2.Id);

                    client2.Send(new Entities.Message()
                        {
                            From = module2.GetMyNode(),
                            To = args.Data.From.Id,
                            Data = new byte[1024]
                        });
                };

            client1.Send(new Entities.Message()
                {
                    From = module1.GetMyNode(),
                    To = module2.Id,
                    Data = new byte[1024],
                });

            Assert.IsTrue(msgEvent.WaitOne(TimeSpan.FromSeconds(2)));


            client1.Send(new Entities.Message()
            {
                From = module1.GetMyNode(),
                To = module2.Id,
                Data = new byte[1024],
            });

            Assert.IsTrue(msgEvent.WaitOne(TimeSpan.FromSeconds(2)));


            module1.Stop();
            module2.Stop();
        }
    }
}
