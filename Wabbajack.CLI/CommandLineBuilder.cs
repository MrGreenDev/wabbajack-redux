using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Wabbajack.CLI.Verbs;

namespace Wabbajack.CLI
{
    public class CommandLineBuilder
    {
        private readonly IEnumerable<IVerb> _verbs;
        private readonly IConsole _console;

        public CommandLineBuilder(IEnumerable<IVerb> verbs, IConsole console)
        {
            _console = console;
            _verbs = verbs;
        }

        public async Task<int> Run(string[] args)
        {
            var root = new RootCommand();
            foreach (var verb in _verbs) 
                root.AddCommand(verb.MakeCommand());
            var builder = new System.CommandLine.Builder.CommandLineBuilder(root);
            var built = builder.Build();
            var parsed = built.Parse(args);
            return await parsed.InvokeAsync(_console);
        }
    }
}