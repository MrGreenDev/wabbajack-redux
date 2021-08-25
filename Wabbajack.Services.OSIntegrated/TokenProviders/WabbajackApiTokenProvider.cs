using System;
using System.IO;
using System.Threading.Tasks;
using Wabbajack.DTOs.Logins;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;

namespace Wabbajack.Services.OSIntegrated.TokenProviders
{
    public class WabbajackApiTokenProvider : ITokenProvider<WabbajackApiState>
    {
        public async ValueTask<WabbajackApiState?> Get()
        {
            if (!MetricsPath.FileExists())
                await CreateMetricsKey();

            return new WabbajackApiState
            {
                MetricsKey = (await MetricsPath.FromEncryptedJsonFile<string>())!,
                AuthorKey = AuthorKeyPath.FileExists() ? await AuthorKeyPath.ReadAllTextAsync() : null
            };
        }

        private async Task CreateMetricsKey()
        {
            var key = MakeRandomKey();
            await key.AsEncryptedJsonFile(MetricsPath);
        }

        private AbsolutePath MetricsPath => KnownFolders.WabbajackAppLocal.Combine("encrypted", "metrics-key");
        private AbsolutePath AuthorKeyPath => KnownFolders.WabbajackAppLocal.Combine("author-api-key.txt");
        
        public static string MakeRandomKey()
        {
            var random = new Random();
            byte[] bytes = new byte[32];
            random.NextBytes(bytes);
            return bytes.ToHex();
        }
        
    }
}