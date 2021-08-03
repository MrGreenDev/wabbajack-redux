using System.CommandLine;
using System.Threading.Tasks;

namespace Wabbajack.CLI.Verbs
{
    public interface IVerb
    {
        public Command MakeCommand();
    }
}