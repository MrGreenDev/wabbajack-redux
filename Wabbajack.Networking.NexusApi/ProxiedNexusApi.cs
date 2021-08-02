using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.Networking.NexusApi.DTOs;

namespace Wabbajack.Networking.NexusApi
{
    public class ProxiedNexusApi : NexusApi
    {
        public ProxiedNexusApi(ApiKey apiKey, ILogger<NexusApi> logger, HttpClient client, ApplicationInfo appInfo, JsonSerializerOptions jsonOptions) 
            : base(apiKey, logger, client, appInfo, jsonOptions)
        {
        }

        public HashSet<string> ProxiedEndpoints = new()
        {
            Endpoints.ModInfo
        };
        
        protected override async ValueTask<HttpRequestMessage> GenerateMessage(HttpMethod method, string uri, params object[] parameters)
        {
            var msg = await base.GenerateMessage(method, uri, parameters);
            if (ProxiedEndpoints.Contains(uri))
                msg.RequestUri = new Uri($"https://build.wabbajack.org/{string.Format(uri, parameters)}");
            return msg;
        }

        protected override ResponseMetadata ParseHeaders(HttpResponseMessage result)
        {
            if (result.RequestMessage!.RequestUri!.Host == "build.wabbajack.org")
                return new ResponseMetadata {IsReal = false};
            return base.ParseHeaders(result);
        }
    }
}