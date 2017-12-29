using System;
using System.Diagnostics;

namespace DI.P2P.Console
{
    using System.Linq;
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


            // Port to bind to.
            int port = 11111;
            var portArg = args.FirstOrDefault(arg => arg.StartsWith("--port="));
            if (portArg != null)
            {
                var portParts = portArg.Split('=');

                if (portParts.Length < 2)
                {
                    Console.Error.WriteLine($"Missing port number");
                    Environment.Exit(1);
                }

                if (!int.TryParse(portParts[1], out port))
                {
                    Console.Error.WriteLine($"Invalid port number {args[0]}");
                    Environment.Exit(1);
                }
            }

            // Port to bind to.
            var configurationDirectory = "conf";
            var confDirArg = args.FirstOrDefault(arg => arg.StartsWith("--conf-dir="));
            if (confDirArg != null)
            {
                var confDirParts = confDirArg.Split('=');

                if (confDirParts.Length < 2)
                {
                    Console.Error.WriteLine($"Missing configuration directory");
                    Environment.Exit(1);
                }

                configurationDirectory = confDirParts[1];
            }


            log.Info("DI.P2P.Console starting..");

            var module = new Module(port, configurationDirectory);

            module.Configure().Wait();
            module.Start().Wait();

            var system = module.Find<P2PSystem>();

            // Addnodes
            foreach (var addnodeArg in args.Where(arg => arg.StartsWith("--addnode=")))
            {
                var addnodePart = addnodeArg.Split('=');

                if (addnodePart.Length == 2)
                {
                    system.AddNode(addnodePart[1]);
                }
            }

            new CommandPrompt(system).Start();

            log.Info("DI.P2P.Console stopping..");

            module.Stop().Wait();

            log.Info("DI.P2P.Console stopped. bye");
        }
    }
}
