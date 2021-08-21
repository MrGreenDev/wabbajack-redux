using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Paths.IO;

namespace Wabbajack.Services.OSIntegrated
{
    public class EncryptedJsonTokenProvider<T> : ITokenProvider<T>
    {
        private readonly ILogger _logger;
        private readonly string _key;

        public EncryptedJsonTokenProvider(ILogger logger, string key)
        {
            _logger = logger;
            _key = key;
        }

        public async ValueTask<T?> Get()
        {
            var path = KnownFolders.WabbajackAppLocal.Combine(_key);
            return await path.FromEncryptedJsonFile<T>();
        }
    }
}