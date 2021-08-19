using System.Threading.Tasks;
using Wabbajack.Networking.Http.Interfaces;

namespace Wabbajack.Networking.NexusApi
{
    public interface ApiKey : ITokenProvider<string>
    {
    }
}