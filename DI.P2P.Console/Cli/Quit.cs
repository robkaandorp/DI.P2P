namespace DI.P2P.Console.Cli
{
    public class Quit : Command
    {
        private readonly CommandPrompt commandPrompt;

        public Quit(CommandPrompt commandPrompt)
        {
            this.commandPrompt = commandPrompt;
        }

        public override bool Execute(CommandLine commandLine)
        {
            this.commandPrompt.Stop();
            return true;
        }
    }
}