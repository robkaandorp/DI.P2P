namespace DI.P2P.Console.Cli
{
    using System;
    using System.Net;

    public class AddNode : Command
    {
        private readonly CommandPrompt commandPrompt;

        public AddNode(CommandPrompt commandPrompt)
        {
            this.commandPrompt = commandPrompt;
        }

        public override bool Execute(CommandLine commandLine)
        {
            if (commandLine.Parameters.Length != 1)
            {
                Console.Error.WriteLine($"Missing parameter");
                Console.Error.WriteLine($"Usage: addnode <ip-address>:<port>");
                return false;
            }

            if (!IPAddress.TryParse(commandLine.Parameters[0].Split(':')[0], out _))
            {
                Console.Error.WriteLine("Incorrect ip-address format");
                return false;
            }

            this.commandPrompt.System.AddNode(commandLine.Parameters[0]);
            return true;
        }
    }
}