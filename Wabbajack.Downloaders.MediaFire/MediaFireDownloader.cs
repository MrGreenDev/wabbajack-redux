using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Wabbajack.Downloaders.Interfaces;
using Wabbajack.DTOs;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.DTOs.Validation;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Paths;
using Wabbajack.RateLimiter;

namespace Wabbajack.Downloaders.MediaFire
{
    public class MediaFireDownloader : ADownloader<DTOs.DownloadStates.MediaFire>, IUrlDownloader
    {
        private readonly ILogger<MediaFireDownloader> _logger;
        private readonly HttpClient _httpClient;
        private readonly IHttpDownloader _downloader;
        private readonly IRateLimiter _limiter;

        public MediaFireDownloader(ILogger<MediaFireDownloader> logger, HttpClient httpClient, IHttpDownloader downloader, IRateLimiter limiter)
        {
            _logger = logger;
            _httpClient = httpClient;
            _downloader = downloader;
            _limiter = limiter;
        }

        public override async Task<Hash> Download(Archive archive, DTOs.DownloadStates.MediaFire state, AbsolutePath destination, CancellationToken token)
        {
            var url = await Resolve(state);
            var msg = new HttpRequestMessage(HttpMethod.Get, url!);
            return await _downloader.Download(msg, destination, token);
        }


        public override Task<bool> Prepare()
        {
            return Task.FromResult(true);
        }

        public override bool IsAllowed(ServerAllowList allowList, IDownloadState state)
        {
            return true;
        }

        public override IDownloadState? Resolve(IReadOnlyDictionary<string, string> iniData)
        {
            if (iniData.ContainsKey("directURL") &&
                Uri.TryCreate(iniData["directURL"], UriKind.Absolute, out var uri)
                && uri.Host == "www.mediafire.com")
            {
                var state = new DTOs.DownloadStates.MediaFire()
                {
                    Url = uri
                };
                return state;
            }

            return null;
        }

        public override Priority Priority => Priority.Normal;
        
        public override async Task<bool> Verify(Archive archive, DTOs.DownloadStates.MediaFire archiveState, CancellationToken token)
        {
            return await Resolve(archiveState, token) != null;
        }
        
        private async Task<Uri?> Resolve(DTOs.DownloadStates.MediaFire state, CancellationToken? token = null)
        {
            token ??= CancellationToken.None;
            using var job = await _limiter.Begin($"Looking for link for MediaFire link {state.Url}", 0, (CancellationToken)token, Resource.Network, Resource.CPU);
            using var result = await _httpClient.GetAsync(state.Url, HttpCompletionOption.ResponseHeadersRead, (CancellationToken)token);
            if (!result.IsSuccessStatusCode)
                return null;

            job.Size = result.Content.Headers.ContentLength ?? 0;

            if (result.Content.Headers.ContentType!.MediaType!.StartsWith("text/html",
                StringComparison.OrdinalIgnoreCase))
            {
                var bodyData = await result.Content.ReadAsStringAsync((CancellationToken)token);
                await job.Report((int)job.Size, (CancellationToken)token);
                var body = new HtmlDocument();
                body.LoadHtml(bodyData);
                var node = body.DocumentNode.DescendantsAndSelf().First(d => d.HasClass("input") && d.HasClass("popsok") &&
                                                                             d.GetAttributeValue("aria-label", "") == "Download file");
                return new Uri(node.GetAttributeValue("href", "not-found"));
            }
            return state.Url;
        }

        public override IEnumerable<string> MetaIni(Archive a, DTOs.DownloadStates.MediaFire state)
        {
            return new[] { $"directURL={state.Url}" };
        }

        public IDownloadState? Parse(Uri uri)
        {
            if (uri.Host != "www.mediafire.com")
                return null;
            return new DTOs.DownloadStates.MediaFire { Url = uri };
        }

        public Uri UnParse(IDownloadState state)
        {
            return ((DTOs.DownloadStates.MediaFire)state).Url;
        }
    }
}

