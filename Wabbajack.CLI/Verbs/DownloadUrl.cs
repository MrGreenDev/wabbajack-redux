using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.Downloaders;
using Wabbajack.DTOs;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;

namespace Wabbajack.CLI.Verbs
{
    
    [Verb("download-url", HelpText = "Downloads a file from a Url and saves it to a file")]
    public class DownloadUrl : AVerb<DownloadUrl>
    {
        private readonly ILogger<DownloadUrl> _logger;
        private readonly DownloadDispatcher _dispatcher;

        public DownloadUrl(ILogger<DownloadUrl> logger, DownloadDispatcher dispatcher) : base(logger)
        {
            _logger = logger;
            _dispatcher = dispatcher;
        }
        
        public async Task<int> Run(
            [Option('u', Required = true, HelpText = "Url to download")]
            Uri url,
            [Option('o', Required = true, HelpText = "Output path")]
            AbsolutePath output)
        {
            var parsed = _dispatcher.Parse(url);
            if (parsed == null)
            {
                _logger.LogCritical("No parser found for {url}", url);
                return 1;
            }

            var hash = await _dispatcher.Download(new Archive { State = parsed! }, output, CancellationToken.None);
            _logger.LogInformation("Downloaded {url}, Size: {size}, Hash: {hash}", url, output.Size(), hash);
            return 0;
        }
        
        
    }
}