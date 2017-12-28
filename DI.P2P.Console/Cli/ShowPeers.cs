namespace DI.P2P.Console.Cli
{
    using System;
    using System.Linq;

    using Akka.Util.Internal;

    public class ShowPeers : Command
    {
        private readonly CommandPrompt commandPrompt;

        public ShowPeers(CommandPrompt commandPrompt)
        {
            this.commandPrompt = commandPrompt;
        }

        public override bool Execute(CommandLine commandLine)
        {
            var peers = this.commandPrompt.System.GetConnectedPeers();

            Console.WriteLine("### Connected peers:");
            peers.ForEach(p => Console.WriteLine($" - {p.Peer}"));
            Console.WriteLine();

            peers = this.commandPrompt.System.GetPeers();

            Console.WriteLine("### Peers registry:");
            peers.ForEach(p => Console.WriteLine($" - {p.Peer} ({p.ConnectionTries} conn. tries)"));
            Console.WriteLine();

            var bannedPeers = this.commandPrompt.System.GetBannedPeers();

            if (peers.Any())
            {
                Console.WriteLine("### Banned peers:");
                bannedPeers.ForEach(p => Console.WriteLine($" - {p.Peer} (until {p.BannedUntil})"));
                Console.WriteLine();
            }

            return true;
        }
    }
}