using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.DTOs;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Networking.NexusApi.DTOs;
using Wabbajack.Server.DTOs;

namespace Wabbajack.Networking.NexusApi
{
    public class NexusApi
    {
        protected readonly ITokenProvider<string> ApiKey;
        private readonly ApplicationInfo _appInfo;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<NexusApi> _logger;

        public NexusApi(ITokenProvider<string> apiKey, ILogger<NexusApi> logger, HttpClient client, ApplicationInfo appInfo,
            JsonSerializerOptions jsonOptions)
        {
            ApiKey = apiKey;
            _logger = logger;
            _client = client;
            _appInfo = appInfo;
            _jsonOptions = jsonOptions;
        }

        public virtual async Task<(ValidateInfo info, ResponseMetadata header)> Validate(
            CancellationToken token = default)
        {
            var msg = await GenerateMessage(HttpMethod.Get, Endpoints.Validate);
            return await Send<ValidateInfo>(msg, token);
        }

        public virtual async Task<(ModInfo info, ResponseMetadata header)> ModInfo(string nexusGameName, long modId,
            CancellationToken token = default)
        {
            var msg = await GenerateMessage(HttpMethod.Get, Endpoints.ModInfo, nexusGameName, modId);
            return await Send<ModInfo>(msg, token);
        }

        public virtual async Task<(ModFiles info, ResponseMetadata header)> ModFiles(string nexusGameName, long modId,
            CancellationToken token = default)
        {
            var msg = await GenerateMessage(HttpMethod.Get, Endpoints.ModFiles, nexusGameName, modId);
            return await Send<ModFiles>(msg, token);
        }

        public virtual async Task<(ModFile info, ResponseMetadata header)> FileInfo(string nexusGameName, long modId,
            long fileId, CancellationToken token = default)
        {
            var msg = await GenerateMessage(HttpMethod.Get, Endpoints.ModFile, nexusGameName, modId, fileId);
            return await Send<ModFile>(msg, token);
        }

        public virtual async Task<(DownloadLink[] info, ResponseMetadata header)> DownloadLink(string nexusGameName,
            long modId, long fileId, CancellationToken token = default)
        {
            var msg = await GenerateMessage(HttpMethod.Get, Endpoints.DownloadLink, nexusGameName, modId, fileId);
            return await Send<DownloadLink[]>(msg, token);
        }

        protected virtual async Task<(T data, ResponseMetadata header)> Send<T>(HttpRequestMessage msg,
            CancellationToken token = default)
        {
            _logger.LogInformation("Nexus Call: {Url}", msg.RequestUri);
            using var result = await _client.SendAsync(msg, token);

            var headers = ParseHeaders(result);

            var body = await result.Content.ReadAsByteArrayAsync(token);
            return (JsonSerializer.Deserialize<T>(body, _jsonOptions)!, headers);
        }

        protected virtual ResponseMetadata ParseHeaders(HttpResponseMessage result)
        {
            var metaData = new ResponseMetadata();

            {
                if (result.Headers.TryGetValues("x-rl-daily-limit", out var limits))
                    if (int.TryParse(limits.First(), out var limit))
                        metaData.DailyLimit = limit;
            }

            {
                if (result.Headers.TryGetValues("x-rl-daily-remaining", out var limits))
                    if (int.TryParse(limits.First(), out var limit))
                        metaData.DailyRemaining = limit;
            }

            {
                if (result.Headers.TryGetValues("x-rl-daily-reset", out var resets))
                    if (DateTime.TryParse(resets.First(), out var reset))
                        metaData.DailyReset = reset;
            }

            {
                if (result.Headers.TryGetValues("x-rl-hourly-limit", out var limits))
                    if (int.TryParse(limits.First(), out var limit))
                        metaData.HourlyLimit = limit;
            }

            {
                if (result.Headers.TryGetValues("x-rl-hourly-remaining", out var limits))
                    if (int.TryParse(limits.First(), out var limit))
                        metaData.HourlyRemaining = limit;
            }

            {
                if (result.Headers.TryGetValues("x-rl-hourly-reset", out var resets))
                    if (DateTime.TryParse(resets.First(), out var reset))
                        metaData.HourlyReset = reset;
            }


            {
                if (result.Headers.TryGetValues("x-runtime", out var runtimes))
                    if (double.TryParse(runtimes.First(), out var reset))
                        metaData.Runtime = reset;
            }

            _logger.LogInformation("Nexus API call finished: {Runtime} - Remaining Limit: {RemainingLimit}",
                metaData.Runtime, Math.Max(metaData.DailyRemaining, metaData.HourlyRemaining));

            return metaData;
        }

        protected virtual async ValueTask<HttpRequestMessage> GenerateMessage(HttpMethod method, string uri,
            params object?[] parameters)
        {
            var msg = new HttpRequestMessage();
            msg.Method = method;
            msg.RequestUri = new Uri($"https://api.nexusmods.com/{string.Format(uri, parameters)}");
            msg.Headers.Add("Application-Name", _appInfo.AppName);
            msg.Headers.Add("Application-Version", _appInfo.AppVersion.ToString());
            msg.Headers.Add("apikey", await ApiKey.Get());
            msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return msg;
        }

        public async Task<(UpdateEntry[], ResponseMetadata headers)> GetUpdates(Game game, CancellationToken token)
        {
            var msg = await GenerateMessage(HttpMethod.Get, Endpoints.Updates, game.MetaData().NexusName, "1m");
            return await Send<UpdateEntry[]>(msg, token);
        }
    }
}