namespace DI.P2P.Console.Cli
{
    using System;
    using System.Net;

    public class BanPeer : Command
    {
        private readonly CommandPrompt commandPrompt;

        public BanPeer(CommandPrompt commandPrompt)
        {
            this.commandPrompt = commandPrompt;
        }

        public override bool Execute(CommandLine commandLine)
        {
            if (commandLine.Parameters.Length < 1 || commandLine.Parameters.Length > 2)
            {
                Console.Error.WriteLine($"Incorrect number of parameters");
                Console.Error.WriteLine($"Usage: banpeer <ip-address> [<port> = 11111]");
                return false;
            }

            if (!IPAddress.TryParse(commandLine.Parameters[0], out _))
            {
                Console.Error.WriteLine("Incorrect ip-address format");
                return false;
            }

            int port = 11111;
            if (commandLine.Parameters.Length > 1 && !int.TryParse(commandLine.Parameters[1], out port))
            {
                Console.Error.WriteLine("Invalid port number");
                return false;
            }

            this.commandPrompt.System.BanPeer(commandLine.Parameters[0], port);
            return true;
        }
    }
}