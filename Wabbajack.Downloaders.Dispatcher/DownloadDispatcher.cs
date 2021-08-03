using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.Downloaders.Interfaces;
using Wabbajack.DTOs;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Paths;
using Xunit.Sdk;

namespace Wabbajack.Downloaders
{
    public class DownloadDispatcher
    {
        private readonly IEnumerable<IDownloader> _downloaders;
        private readonly ILogger<DownloadDispatcher> _logger;

        public DownloadDispatcher(ILogger<DownloadDispatcher> logger, IEnumerable<IDownloader> downloaders)
        {
            _downloaders = downloaders;
            _logger = logger;
        }

        public async Task<Hash> Download(Archive a, AbsolutePath dest, CancellationToken token)
        {
            foreach (var downloader in _downloaders)
            {
                if (downloader.CanDownload(a))
                    return await downloader.Download(a, dest, token);
            }

            throw new NotImplementedException();
        }
        
        public async Task<bool> Verify(Archive a, CancellationToken token)
        {
            foreach (var downloader in _downloaders)
            {
                if (downloader.CanDownload(a))
                    return await downloader.Verify(a, token);
            }

            throw new NotImplementedException();
        }
    }
}