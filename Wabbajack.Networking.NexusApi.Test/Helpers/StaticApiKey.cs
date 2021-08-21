using System.Threading.Tasks;

namespace Wabbajack.Networking.NexusApi.Test.Helpers
{
    public class StaticApiKey : ApiKey
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