using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;

namespace Wabbajack.Networking.Http
{
    public class SingleThreadedDownloader : IHttpDownloader
    {
        private readonly ILogger<SingleThreadedDownloader> _logger;

        public SingleThreadedDownloader(ILogger<SingleThreadedDownloader> logger)
        {
            _logger = logger;
        }

        public async Task<Hash> Download(HttpResponseMessage response, AbsolutePath outputPath, CancellationToken token)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogCritical("Can't download a unsuccessful response, got {status} {reason}",
                    response.StatusCode, response.ReasonPhrase);
                return default;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(token);
            await using var outputStream = outputPath.Open(FileMode.Create, FileAccess.Write);
            return await stream.HashingCopy(outputStream, token);
        }
    }
}