using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CG.Web.MegaApiClient;
using Microsoft.Extensions.Logging;
using Wabbajack.Downloaders.Interfaces;
using Wabbajack.DTOs;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.DTOs.Validation;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Wabbajack.RateLimiter;

namespace Wabbajack.Downloaders.ModDB
{
    public class MegaDownloader : ADownloader<Mega>, IUrlDownloader
    {
        private readonly ILogger<MegaDownloader> _logger;
        private readonly MegaApiClient _apiClient;
        private readonly IRateLimiter _limiter;
        private const string MegaPrefix = "https://mega.nz/#!";
        private const string MegaFilePrefix = "https://mega.nz/file/";

        public MegaDownloader(ILogger<MegaDownloader> logger, MegaApiClient apiClient, IRateLimiter limiter)
        {
            _logger = logger;
            _apiClient = apiClient;
            _limiter = limiter;
        }
        public override async Task<Hash> Download(Archive archive, Mega state, AbsolutePath destination, CancellationToken token)
        {
            using var job = await _limiter.Begin($"Downloading {destination.FileName}", archive.Size, token,
                Resource.Disk, Resource.Network, Resource.CPU);
            
            if (!_apiClient.IsLoggedIn)
                await _apiClient.LoginAsync();

            await using var ous = destination.Open(FileMode.Create, FileAccess.Write, FileShare.None);
            await using var ins = await _apiClient.DownloadAsync(state.Url, cancellationToken:token);
            return await ins.HashingCopy(ous, token, job);
        }

        private Mega? GetDownloaderState(string? url)
        {
            if (url == null) return null;
            
            if ((url.StartsWith(MegaPrefix) || url.StartsWith(MegaFilePrefix)))
                return new Mega{Url = new Uri(url)};
            return null;
        }

        public override async Task<bool> Prepare()
        {
            if (!_apiClient.IsLoggedIn)
                await _apiClient.LoginAsync();
            return true;
        }

        public override bool IsAllowed(ServerAllowList allowList, IDownloadState state)
        {
            var megaState = (Mega)state;
            return allowList.AllowedPrefixes.Any(p => megaState.Url.ToString().StartsWith(p));
        }

        public override IDownloadState? Resolve(IReadOnlyDictionary<string, string> iniData)
        {
            return iniData.ContainsKey("directURL") ? GetDownloaderState(iniData["directURL"]) : null;
        }

        public override Priority Priority => Priority.Normal;
        public override async Task<bool> Verify(Archive archive, Mega archiveState, CancellationToken token)
        {
            if (!_apiClient.IsLoggedIn)
                await _apiClient.LoginAsync();
            
            try
            {
                var node = await _apiClient.GetNodeFromLinkAsync(archiveState.Url);
                return node != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override IEnumerable<string> MetaIni(Archive a, Mega state)
        {
            return new[] { $"directURL={state.Url}" };
        }

        public IDownloadState? Parse(Uri uri)
        {
            return GetDownloaderState(uri.ToString());
        }

        public Uri UnParse(IDownloadState state)
        {
            return ((Mega)state).Url;
        }
    }
}