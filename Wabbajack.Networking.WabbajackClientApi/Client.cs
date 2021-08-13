﻿using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.DTOs;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.DTOs.Validation;
using Wabbajack.Hashing.xxHash64;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Wabbajack.Networking.WabbajackClientApi
{
    public class Client
    {
        private readonly HttpClient _client;

        private readonly Configuration _configuration;
        private readonly ILogger<Client> _logger;
        private readonly DTOSerializer _dtos
            ;

        public Client(ILogger<Client> logger, HttpClient client, Configuration configuration, DTOSerializer dtos)
        {
            _configuration = configuration;
            _client = client;
            _logger = logger;
            _logger.LogInformation("File hash check (-42) {key}", _configuration.MetricsKey);
            _dtos = dtos;
        }

        public async Task SendMetric(string action, string subject)
        {
            var msg = new HttpRequestMessage(HttpMethod.Get,
                $"{_configuration.BuildServerUrl}metrics/{action}/{subject}");
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
        
        public async Task<Archive[]> GetGameArchives(Game game, string version)
        {
            var url = $"https://raw.githubusercontent.com/wabbajack-tools/indexed-game-files/master/{game}/{version}.json";
            return await _client.GetFromJsonAsync<Archive[]>(url) ?? Array.Empty<Archive>();
        }
        
        public async Task<Archive[]> GetArchivesForHash(Hash hash)
        {
            return await _client.GetFromJsonAsync<Archive[]>(
                $"{_configuration.BuildServerUrl}mod_files/by_hash/{hash.ToHex()}") ?? Array.Empty<Archive>();
        }

        public async Task<Uri?> GetMirrorUrl(Hash archiveHash)
        {
            try
            {
                var result =
                    await _client.GetStringAsync($"{_configuration.BuildServerUrl}mirror/{archiveHash.ToHex()}");
                return new Uri(result);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "While downloading mirror for {hash}", archiveHash);
                return null;
            }
        }

        public async Task SendModListDefinition(ModList modList)
        {

            await using var fs = new MemoryStream();
            await using var gzip = new GZipStream(fs, CompressionLevel.SmallestSize, true);
            await _dtos.Serialize(modList, gzip);
            await gzip.DisposeAsync();
            fs.Position = 0;

            var msg = new HttpRequestMessage(HttpMethod.Post,
                $"{_configuration.BuildServerUrl}list_definitions/ingest");
            msg.Headers.Add("x-compressed-body", "gzip");
            msg.Content = new StreamContent(fs);
            await _client.SendAsync(msg);
        }
    }
}