using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.Downloaders.Interfaces;
using Wabbajack.DTOs;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.DTOs.Validation;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Paths;

namespace Wabbajack.Downloaders.Http
{
    public class HttpDownloader : ADownloader<DTOs.DownloadStates.Http>, IUrlDownloader
    {
        private readonly HttpClient _client;
        private readonly IHttpDownloader _downloader;
        private readonly ILogger<HttpDownloader> _logger;

        public HttpDownloader(ILogger<HttpDownloader> logger, HttpClient client, IHttpDownloader downloader)
        {
            _client = client;
            _logger = logger;
            _downloader = downloader;
        }

        public override async Task<Hash> Download(Archive archive, DTOs.DownloadStates.Http state,
            AbsolutePath destination, CancellationToken token)
        {
            return await _downloader.Download(await GetResponse(state, token), destination, token);
        }

        private async Task<HttpResponseMessage> GetResponse(DTOs.DownloadStates.Http state, CancellationToken token)
        {
            var msg = new HttpRequestMessage(HttpMethod.Get, state.Url);
            foreach (var header in state.Headers)
            {
                var kw = header.Split(":").Select(s => s.Trim()).ToArray();
                msg.Headers.Add(kw[0], kw[1]);
            }

            return await _client.SendAsync(msg, token);
        }

        public override IDownloadState? Resolve(IReadOnlyDictionary<string, string> iniData)
        {
            if (iniData.ContainsKey("directURL") && Uri.TryCreate(iniData["directURL"], UriKind.Absolute, out var uri))
            {
                var state = new DTOs.DownloadStates.Http
                {
                    Url = uri
                };
                
                if (iniData.TryGetValue("directURLHeaders", out var headers))
                {
                    state.Headers = headers.Split("|").ToArray();
                }

                return state;
            }

            return null;
        }

        public override async Task<bool> Verify(Archive archive, DTOs.DownloadStates.Http archiveState,
            CancellationToken token)
        {
            var response = await GetResponse(archiveState, token);
            if (!response.IsSuccessStatusCode) return false;

            var headerVar = archive.Size == 0 ? "1" : archive.Size.ToString();
            ulong headerContentSize = 0;
            if (response.Content.Headers.Contains("Content-Length"))
            {
                headerVar = response.Content.Headers.GetValues("Content-Length").FirstOrDefault();
                if (headerVar != null)
                    if (!ulong.TryParse(headerVar, out headerContentSize))
                        return true;
            }

            response.Dispose();
            if (archive.Size != 0 && headerContentSize != 0)
                return archive.Size == (long)headerContentSize;
            return true;
        }

        public override async Task<bool> Prepare()
        {
            return true;
        }

        public override bool IsAllowed(ServerAllowList allowList, IDownloadState state)
        {
            return allowList.AllowedPrefixes.Any(p => ((DTOs.DownloadStates.Http)state).Url.ToString().StartsWith(p));
        }

        public override IEnumerable<string> MetaIni(Archive a, DTOs.DownloadStates.Http state)
        {
            if (state.Headers.Length > 0)
                return new[]
                {
                    $"directURL={state.Url}",
                    $"directURLHeaders={string.Join("|", state.Headers)}"
                };
            return new[] { $"directURL={state.Url}" };
        }

        public IDownloadState? Parse(Uri uri)
        {
            return new DTOs.DownloadStates.Http { Url = uri };
        }

        public Uri UnParse(IDownloadState state)
        {
            return ((DTOs.DownloadStates.Http)state).Url;
        }
        
        public override Priority Priority => Priority.Low;
    }
}