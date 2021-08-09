using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Wabbajack.Downloaders.Interfaces;
using Wabbajack.DTOs;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.DTOs.Validation;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Networking.NexusApi;
using Wabbajack.Paths;

namespace Wabbajack.Downloaders
{
    public class NexusDownloader : ADownloader<Nexus>, IUrlDownloader
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

        public override async Task<bool> Prepare()
        {
            return true;
        }

        public override bool IsAllowed(ServerAllowList allowList, IDownloadState state)
        {
            return true;
        }

        public override IEnumerable<string> MetaIni(Archive a, Nexus state)
        {
            return new[] {$"gameName={state.Game.MetaData().MO2ArchiveName}", $"modID={state.ModID}", $"fileID={state.FileID}"};
        }

        public IDownloadState? Parse(Uri uri)
        {
            if (uri.Host != "www.nexusmods.com")
                return null;
            var relPath = (RelativePath)uri.AbsolutePath;
            long modId, fileId;

            if (relPath.Depth != 3)
            {
                _logger.LogWarning("Got www.nexusmods.com link but it didn't match a parsable pattern: {url}", uri);
                return null;
            }

            if (!long.TryParse(relPath.FileName.ToString(), out modId))
                return null;

            var game = GameRegistry.ByNexusName[relPath.Parent.Parent.ToString()].FirstOrDefault();
            if (game == null) return null;

            var query = HttpUtility.ParseQueryString(uri.Query);
            var fileIdStr = query.Get("file_id");
            if (!long.TryParse(fileIdStr, out fileId))
                return null;

            return new Nexus
            {
                Game = game.Game,
                ModID = modId,
                FileID = fileId
            };
        }

        public Uri UnParse(IDownloadState state)
        {
            var nstate = (Nexus)state;
            return new Uri(
                $"https://www.nexusmods.com/{nstate.Game.MetaData().NexusName}/mods/{nstate.ModID}/?tab=files&file_id={nstate.FileID}");
        }
    }
}