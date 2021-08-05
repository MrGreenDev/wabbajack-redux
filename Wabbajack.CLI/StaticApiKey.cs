using System.Threading.Tasks;
using Wabbajack.Networking.NexusApi;

namespace Wabbajack.CLI
{
    public class StaticApiKey : ApiKey
    {
        private readonly string _key;

        public StaticApiKey(string key)
        {
            _key = key;
        }

        public ValueTask<string> GetKey()
        {
            return new(_key);
        }
    }
}