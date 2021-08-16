using System.Threading.Tasks;
using Wabbajack.Networking.Http.Interfaces;

namespace Wabbajack.Networking.GitHub
{
    public abstract class GithubAuthTokenProvider : ITokenProvider<string>
    {
        public abstract ValueTask<string> Get();
    }

}