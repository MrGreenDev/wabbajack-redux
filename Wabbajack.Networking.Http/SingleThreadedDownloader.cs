using System;
using System.Diagnostics;
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
            await using var stream = await response.Content.ReadAsStreamAsync();
            var buffer = new byte[1024 * 1024];

            var hasher = new xxHashAlgorithm();

            await using var output = outputPath.Open(FileMode.Create, FileAccess.Write);

            var running = true;
            ulong finalHash = 0;
            var stopWatch = new Stopwatch();
            while (running && !token.IsCancellationRequested)
            {
                var totalRead = 0;

                while (totalRead != buffer.Length)
                {
                    var read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead, token);
                    if (read == 0)
                    {
                        running = false;
                        break;
                    }

                    totalRead += read;
                }
                
                var pendingWrite = output.WriteAsync(buffer, 0, totalRead, token);
                if (totalRead != buffer.Length)
                {
                    finalHash = hasher.FinalizeHashValueInternal(buffer[..totalRead]);
                }
                else
                {
                    hasher.TransformByteGroupsInternal(buffer);
                }

                await pendingWrite;
            }

            return new Hash(finalHash);
        }
    }
}