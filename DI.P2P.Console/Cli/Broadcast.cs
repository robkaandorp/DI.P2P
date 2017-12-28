namespace DI.P2P.Console.Cli
{
    using System;
    using System.Net;

    public class Broadcast : Command
    {
        private readonly CommandPrompt commandPrompt;

        public Broadcast(CommandPrompt commandPrompt)
        {
            this.commandPrompt = commandPrompt;
        }

        public override bool Execute(CommandLine commandLine)
        {
            if (commandLine.Parameters.Length != 1)
            {
                Console.Error.WriteLine($"Missing parameter");
                Console.Error.WriteLine($"Usage: broadcast <test-string>");
                return false;
            }

            try
            {
                this.commandPrompt.System.Broadcast(commandLine.Parameters[0]);
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