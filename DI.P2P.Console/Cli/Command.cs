namespace DI.P2P.Console.Cli
{
    public abstract class Command
    {
        public abstract bool Execute(CommandLine commandLine);
    }
}