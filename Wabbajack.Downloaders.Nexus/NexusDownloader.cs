using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.Downloaders.Interfaces;
using Wabbajack.DTOs;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Networking.NexusApi;
using Wabbajack.Paths;

namespace Wabbajack.Downloaders
{
    public class NexusDownloader : ADownloader<Nexus>
    {
        private readonly ILogger<NexusDownloader> _logger;
        private readonly HttpClient _client;
        private readonly IHttpDownloader _downloader;
        private readonly NexusApi _api;

        public NexusDownloader(ILogger<NexusDownloader> logger, HttpClient client, IHttpDownloader downloader, NexusApi api)
        {
            _logger = logger;
            _client = client;
            _downloader = downloader;
            _api = api;
        }
        public override async Task<Hash> Download(Archive archive, Nexus state, AbsolutePath destination, CancellationToken token)
        {
            var urls = await _api.DownloadLink(state.Game.MetaData().NexusName!, state.ModID, state.FileID, token);
            _logger.LogInformation("Downloading Nexus File: {game}|{modid}|{fileid}",state.Game, state.ModID, state.FileID);
            var response = await _client.GetAsync(urls.info.First().URI, token);
            return await _downloader.Download(response, destination, token);
        }

        public override async Task<bool> Verify(Archive archive, Nexus state, CancellationToken token)
        {
            var fileInfo = await _api.FileInfo(state.Game.MetaData().NexusName!, state.ModID, state.FileID, token);
            return fileInfo.info.FileId == state.FileID;
        }
    }
}