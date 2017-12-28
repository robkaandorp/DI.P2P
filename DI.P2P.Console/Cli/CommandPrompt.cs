namespace DI.P2P.Console.Cli
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class CommandPrompt
    {
        public P2PSystem System { get; }

        public CommandPrompt(P2PSystem system)
        {
            this.System = system;
            this.InitializeCommandMap();
        }

        private bool keepRunning = false;

        public void Start()
        {
            this.keepRunning = true;

            while (this.keepRunning)
            {
                Console.WriteLine();
                Console.Write("> ");

                string line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var commandLine = this.ParseLine(line);

                if (!this.commandMap.ContainsKey(commandLine.CommandName))
                {
                    Console.Error.WriteLine("Invalid command");
                    continue;
                }

                var result = this.commandMap[commandLine.CommandName]().Execute(commandLine);
            }
        }

        public void Stop()
        {
            this.keepRunning = false;
        }

        private CommandLine ParseLine(string line)
        {
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return new CommandLine
                       {
                           CommandName = tokens[0],
                           Parameters = tokens.Skip(1).ToArray()
                       };
        }

        private Command CreateCommand<T>()
            where T: Command
        {
            return (T)Activator.CreateInstance(typeof(T), this);
        }

        private Dictionary<string, Func<Command>> commandMap;

        private void InitializeCommandMap()
        {
            this.commandMap = new Dictionary<string, Func<Command>>
                {
                    { "help", this.CreateCommand<Help> },
                    { "exit", this.CreateCommand<Quit> },
                    { "quit", this.CreateCommand<Quit> },
                    { "x", this.CreateCommand<Quit> },
                    { "q", this.CreateCommand<Quit> },
                    { "showpeers", this.CreateCommand<ShowPeers> },
                    { "addnode", this.CreateCommand<AddNode> },
                    { "banpeer", this.CreateCommand<BanPeer> },
                };
        }

        public string[] GetCommands()
        {
            return this.commandMap.Keys.ToArray();
        }
    }
}
