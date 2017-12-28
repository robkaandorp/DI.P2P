namespace DI.P2P.Console.Cli
{
    using System;
    using System.Net;

    public class Ping : Command
    {
        private readonly CommandPrompt commandPrompt;

        public Ping(CommandPrompt commandPrompt)
        {
            this.commandPrompt = commandPrompt;
        }

        public override bool Execute(CommandLine commandLine)
        {
            if (commandLine.Parameters.Length != 1)
            {
                Console.Error.WriteLine($"Missing parameter");
                Console.Error.WriteLine($"Usage: ping <ip-address>:<port>");
                return false;
            }

            if (!IPAddress.TryParse(commandLine.Parameters[0].Split(':')[0], out _))
            {
                Console.Error.WriteLine("Incorrect ip-address format");
                return false;
            }

            try
            {
                var elapsed = this.commandPrompt.System.Ping(commandLine.Parameters[0]);
                Console.WriteLine($"Pong received; response time: {elapsed}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return false;
            }

            return true;
        }
    }
}