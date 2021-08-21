using System.Threading.Tasks;
using Wabbajack.Networking.Http.Interfaces;

namespace Wabbajack.Networking.NexusApi.Test.Helpers
{
    public class StaticApiKey : ITokenProvider<string>
    {
        private readonly string _key;

        public StaticApiKey(string key)
        {
            _key = key;
        }

        public ValueTask<string> Get()
        {
            return new ValueTask<string>(_key);
        }
    }
}