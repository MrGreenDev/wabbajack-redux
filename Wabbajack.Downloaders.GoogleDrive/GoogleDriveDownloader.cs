using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.Downloaders.Interfaces;
using Wabbajack.DTOs;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Networking.Http;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Paths;

namespace Wabbajack.Downloaders.GoogleDrive
{
    public class GoogleDriveDownloader : ADownloader<DTOs.DownloadStates.GoogleDrive>, IUrlDownloader
    {
        private readonly ILogger<GoogleDriveDownloader> _logger;
        private readonly HttpClient _client;
        private readonly IHttpDownloader _downloader;

        public GoogleDriveDownloader(ILogger<GoogleDriveDownloader> logger, HttpClient client, IHttpDownloader downloader)
        {
            _logger = logger;
            _client = client;
            _downloader = downloader;
        }

        public override async Task<Hash> Download(Archive archive, DTOs.DownloadStates.GoogleDrive state, AbsolutePath destination, CancellationToken token)
        {
            var msg = await ToMessage(state, true, token);
            using var response = await _client.SendAsync(msg!, token);
            return await _downloader.Download(response!, destination, token);
        }

        public override async Task<bool> Verify(Archive archive, DTOs.DownloadStates.GoogleDrive state, CancellationToken token)
        {
            var result = await ToMessage(state, false, token);
            return result != null;
        }
        
        private async Task<HttpRequestMessage?> ToMessage(DTOs.DownloadStates.GoogleDrive state, bool download, CancellationToken token)
        {
            if (download)
            {
                var initialUrl = $"https://drive.google.com/uc?id={state.Id}&export=download";
                using var response = await _client.GetAsync(initialUrl, token);
                var cookies = response.GetSetCookies();
                var warning = cookies.FirstOrDefault(c => c.Key.StartsWith("download_warning_"));
                response.Dispose();
                if (warning == default)
                {
                    return new HttpRequestMessage(HttpMethod.Get, initialUrl);
                }

                var url = $"https://drive.google.com/uc?export=download&confirm={warning.Value}&id={state.Id}";
                var httpState = new HttpRequestMessage(HttpMethod.Get, url);
                return httpState;
            }
            else
            {
                var url = $"https://drive.google.com/file/d/{state.Id}/edit";
                using var response = await _client.GetAsync(url, token);
                return !response.IsSuccessStatusCode ? null : new HttpRequestMessage(HttpMethod.Get, url);
            }
        }

        private static readonly Regex GDriveRegex = new("((?<=id=)[a-zA-Z0-9_-]*)|(?<=\\/file\\/d\\/)[a-zA-Z0-9_-]*", RegexOptions.Compiled);
        public IDownloadState? Parse(Uri uri)
        {
            if (uri.Host != "drive.google.com") return null;
            var match = GDriveRegex.Match(uri.ToString());
            if (match.Success) 
                return new DTOs.DownloadStates.GoogleDrive { Id = match.ToString() };
            _logger.LogWarning($"Tried to parse drive.google.com Url but couldn't get an id from: {uri}");
            return null;

        }

        public Uri UnParse(IDownloadState state)
        {
            return new Uri(
                $"https://drive.google.com/uc?id={(state as DTOs.DownloadStates.GoogleDrive)?.Id}&export=download");
        }
    }
}