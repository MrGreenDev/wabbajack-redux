using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.DTOs;
using Wabbajack.DTOs.Logins;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Networking.NexusApi.DTOs;
using Wabbajack.Networking.WabbajackClientApi;

namespace Wabbajack.Networking.NexusApi
{
    public class ProxiedNexusApi : NexusApi
    {
        public HashSet<string> ProxiedEndpoints = new()
        {
            Endpoints.ModInfo,
            Endpoints.ModFiles,
            Endpoints.ModFile
        };

        private readonly ITokenProvider<WabbajackApiState> _apiState;
        private readonly Configuration _wabbajackClientConfiguration;

        public ProxiedNexusApi(ITokenProvider<NexusApiState> apiKey, ILogger<NexusApi> logger, HttpClient client, ApplicationInfo appInfo,
            JsonSerializerOptions jsonOptions, ITokenProvider<WabbajackApiState> apiState, Configuration wabbajackClientConfiguration)
            : base(apiKey, logger, client, appInfo, jsonOptions)
        {
            _apiState = apiState;
            _wabbajackClientConfiguration = wabbajackClientConfiguration;
        }

        protected override async ValueTask<HttpRequestMessage> GenerateMessage(HttpMethod method, string uri,
            params object[] parameters)
        {
            var msg = await base.GenerateMessage(method, uri, parameters);
            if (ProxiedEndpoints.Contains(uri))
                msg.RequestUri = new Uri($"https://build.wabbajack.org/{string.Format(uri, parameters)}");
            msg.Headers.Add(_wabbajackClientConfiguration.MetricsKeyHeader, (await _apiState.Get())!.MetricsKey);
            return msg;
        }

        protected override ResponseMetadata ParseHeaders(HttpResponseMessage result)
        {
            if (result.RequestMessage!.RequestUri!.Host == "build.wabbajack.org")
                return new ResponseMetadata { IsReal = false };
            return base.ParseHeaders(result);
        }
    }
}