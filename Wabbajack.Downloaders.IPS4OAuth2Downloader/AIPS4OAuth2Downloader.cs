using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.Common;
using Wabbajack.Downloaders.Interfaces;
using Wabbajack.DTOs;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.DTOs.Logins;
using Wabbajack.DTOs.Validation;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Networking.Http;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Paths;

namespace Wabbajack.Downloaders.IPS4OAuth2Downloader
{
    public class AIPS4OAuth2Downloader<TDownloader, TLogin, TState> : ADownloader<TState>
        where TLogin : OAuth2LoginState
    where TState : IPS4OAuth2, new()
    {
        private readonly ILogger _logger;
        private readonly ITokenProvider<TLogin> _loginInfo;
        private readonly HttpClient _client;
        private readonly IHttpDownloader _downloader;
        private readonly Uri _siteURL;
        private readonly ApplicationInfo _appInfo;
        private readonly string _siteName;


        public AIPS4OAuth2Downloader(ILogger logger, ITokenProvider<TLogin> loginInfo, HttpClient client, 
            IHttpDownloader downloader, ApplicationInfo appInfo, Uri siteURL, string siteName)
        {
            _logger = logger;
            _loginInfo = loginInfo;
            _client = client;
            _downloader = downloader;
            _siteURL = siteURL;
            _appInfo = appInfo;
            _siteName = siteName;
        }

        public async ValueTask<HttpRequestMessage> MakeMessage(HttpMethod method, Uri url)
        {
            var msg = new HttpRequestMessage(method, url);
            var loginData = await _loginInfo.Get();
            msg.AddCookiesAndAgent(loginData.Cookies);
            msg.Headers.Add("Authorization", $"Bearer {loginData.ResultState.AccessToken}");
            return msg;
        }

        public async Task<IPS4OAuthFilesResponse.Root> GetDownloads(long modID, CancellationToken token)
        {
            var url = new Uri(_siteURL + $"api/downloads/files/{modID}");
            var msg = await MakeMessage(HttpMethod.Get, url);
            using var response = await _client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, token);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<IPS4OAuthFilesResponse.Root>(cancellationToken: token))!;

            _logger.LogCritical("IPS4 Request Error {response} {reason} - \n {url}", response.StatusCode, response.ReasonPhrase, url);
            throw new HttpException(response);
        }
        
        public override async Task<Hash> Download(Archive archive, TState state, AbsolutePath destination, CancellationToken token)
        {
            if (state.IsAttachment)
            {
                var msg = await MakeMessage(HttpMethod.Get,
                    new Uri($"{_siteURL}/applications/core/interface/file/attachment.php?id={state.IPS4Mod}"));
                using var response = await _client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, token);
                return await _downloader.Download(response, destination, token);
            }
            else
            {
                var downloads = await GetDownloads(state.IPS4Mod, token);
                var fileEntry = downloads.Files.FirstOrDefault(f => f.Name == state.IPS4File);
                var msg = new HttpRequestMessage(HttpMethod.Get, fileEntry.Url);
                msg.Headers.Add("User-Agent", _appInfo.UserAgent);
                using var response =
                    await _client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, token);
                return await _downloader.Download(response, destination, token);
            }
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
            if (!iniData.ContainsKey("ips4Site") || iniData["ips4Site"] != _siteName) return null;
            
            if (iniData.ContainsKey("ips4Mod") && iniData.ContainsKey("ips4File"))
            {
                if (!long.TryParse(iniData["ips4Mod"], out var parsedMod))
                    return null;
                var state = new TState {IPS4Mod = parsedMod, IPS4File = iniData["ips4File"]};
                return state;
            }
            
            if (iniData.ContainsKey("ips4Attachment") != default)
            {
                if (!long.TryParse(iniData["ips4Attachment"], out var parsedMod)) 
                    return null;
                var state = new TState
                {
                    IPS4Mod = parsedMod, 
                    IsAttachment = true,
                    IPS4Url=$"{_siteURL}/applications/core/interface/file/attachment.php?id={parsedMod}"
                };

                return state;
            }

            return null;
        }

        public override Priority Priority => Priority.Normal;
        public override async Task<bool> Verify(Archive archive, TState state, CancellationToken token)
        {
            if (state.IsAttachment)
            {
                var msg = await MakeMessage(HttpMethod.Get,
                    new Uri($"{_siteURL}/applications/core/interface/file/attachment.php?id={state.IPS4Mod}"));
                using var response = await _client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, token);
                return response.IsSuccessStatusCode;

            }
            else
            {
                var downloads = await GetDownloads(state.IPS4Mod, token);
                var fileEntry = downloads.Files.FirstOrDefault(f => f.Name == state.IPS4File);
                if (fileEntry == null) return false;
                return archive.Size == 0 || fileEntry.Size == archive.Size;
            }
        }

        public override IEnumerable<string> MetaIni(Archive a, TState state)
        {
            return new[]
            {
                $"ips4Site={_siteName}", 
                $"ips4Mod={state.IPS4Mod}",
                $"ips4File={state.IPS4File}"
            };
        }
    }
}