using System.Threading.Tasks;

namespace Wabbajack.Networking.NexusApi
{
    public interface ApiKey
    {
        public ValueTask<string> GetKey();
    }
}