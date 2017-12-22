namespace DI.P2P.Console.Cli
{
    using System;

    public class Help : Command
    {
        private readonly CommandPrompt commandPrompt;

        public Help(CommandPrompt commandPrompt)
        {
            this.commandPrompt = commandPrompt;
        }

        public override bool Execute(CommandLine commandLine)
        {
            var commands = this.commandPrompt.GetCommands();
            Console.WriteLine($"Available commands are: {string.Join(", ", commands)}");
            return true;
        }
    }
}