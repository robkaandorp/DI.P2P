using System;
using System.Diagnostics;

namespace DI.P2P.Console
{
    using System.Reflection;

    using Akka.Util.Internal;

    using DI.P2P.Console.Cli;

    using log4net;
    using log4net.Config;
    using log4net.Core;
    using log4net.Repository.Hierarchy;

    using Console = System.Console;
    using Module = DI.P2P.Module;

    class Program
    {
        static void Main(string[] args)
        {
            // Set up a simple configuration that logs on the console.
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            BasicConfigurator.Configure(logRepository);

            var log = LogManager.GetLogger(typeof(Program));

            int port = 11111;
            if (args.Length > 0)
            {
                // Port to bind to.
                if (!int.TryParse(args[0], out port))
                {
                    Console.Error.WriteLine($"Invalid port number {args[0]}");
                    Environment.Exit(1);
                }
            }


            log.Info("DI.P2P.Console starting..");

            var module = new Module(Guid.NewGuid(), port);

            module.Configure().Wait();
            module.Start().Wait();

            var system = module.Find<P2PSystem>();

            if (args.Length > 1)
            {
                // Addnode.
                system.AddNode(args[1]);
            }

            new CommandPrompt(system).Start();

            log.Info("DI.P2P.Console stopping..");

            module.Stop().Wait();

            log.Info("DI.P2P.Console stopped. bye");
        }
    }
}
