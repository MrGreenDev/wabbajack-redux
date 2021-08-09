using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.DTOs.Validation;
using Wabbajack.Hashing.xxHash64;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Wabbajack.Networking.WabbajackClientApi
{
    public class Client
    {

        private readonly Configuration _configuration;
        private readonly HttpClient _client;
        private readonly ILogger<Client> _logger;

        public Client(ILogger<Client> logger, HttpClient client, Configuration configuration)
        {
            _configuration = configuration;
            _client = client;
            _logger = logger;
            _logger.LogInformation("File hash check (-42) {key}", _configuration.MetricsKey);
        }
        
        public async Task SendMetric(string action, string subject)
        {
            var msg = new HttpRequestMessage(HttpMethod.Get, $"{_configuration.BuildServerUrl}metrics/{action}/{subject}");
            msg.Headers.Add(_configuration.MetricsKeyHeader, _configuration.MetricsKey);
            await _client.SendAsync(msg);
        }

        public async Task<ServerAllowList> LoadAllowList()
        {
            var str = await _client.GetStringAsync(_configuration.ServerAllowList);
            var d = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();
            return d.Deserialize<ServerAllowList>(str);
        }

        public async Task<Uri?> GetMirrorUrl(Hash archiveHash)
        {
            try
            {
                var result = await _client.GetStringAsync($"{_configuration.BuildServerUrl}mirror/{archiveHash.ToHex()}");
                return new Uri(result);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "While downloading mirror for {hash}", archiveHash);
                return null;
            }
        }
    }
}