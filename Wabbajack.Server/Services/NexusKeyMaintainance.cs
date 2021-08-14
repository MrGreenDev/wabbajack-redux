using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Discord.Net;
using Microsoft.Extensions.Logging;
using Wabbajack.BuildServer;
using Wabbajack.Networking.NexusApi;
using Wabbajack.Networking.NexusApi.DTOs;
using Wabbajack.Server.DataLayer;

namespace Wabbajack.Server.Services
{
    public class NexusKeyMaintainance : AbstractService<NexusKeyMaintainance, int>
    {
        private SqlService _sql;
        private string _selfKey;

        public NexusKeyMaintainance(ILogger<NexusKeyMaintainance> logger, AppSettings settings, SqlService sql, QuickSync quickSync) 
            : base(logger, settings, quickSync, TimeSpan.FromHours(4))
        {
            _sql = sql;
        }

        public async Task<NexusApi> GetClient()
        {
            var keys = await _sql.GetNexusApiKeysWithCounts(1500);
            foreach (var key in keys.Where(k => k.Key != _selfKey))
            {
                try
                {
                    var client = new TrackingClient(_sql, key);
                    if (await client.IsPremium())
                        return client;

                    _logger.LogWarning($"Purging non premium key");
                    await _sql.DeleteNexusAPIKey(key.Key);
                    continue;
                }
                catch (Exception ex)
                {
                    Utils.Log($"Error getting tracking client: {ex}");
                }

            }

            var bclient = await NexusApiClient.Get();
            await bclient.GetUserStatus();
            return bclient;
        }
        
        public override async Task<int> Execute()
        {
            _selfKey ??= await Utils.FromEncryptedJson<string>("nexusapikey");
            var keys = await _sql.GetNexusApiKeysWithCounts(0);
            _logger.Log(LogLevel.Information, $"Verifying {keys.Count} API Keys");
            foreach (var key in keys)
            {
                try
                {
                    var client = new TrackingClient(_sql, key);

                    var status = await client.GetUserStatus();
                    if (!status.is_premium)
                    {
                        await _sql.DeleteNexusAPIKey(key.Key);
                        continue;
                    }

                    var (daily, hourly) = await client.GetRemainingApiCalls();
                    await _sql.SetNexusAPIKey(key.Key, daily, hourly);
                }
                catch (HttpException ex)
                {
                    _logger.Log(LogLevel.Warning, $"Nexus error, not purging API key : {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Warning, $"Update error, purging API key : {ex.Message}");
                    await _sql.DeleteNexusAPIKey(key.Key);
                }
            }
            return keys.Count;
        }
    }

    public class TrackingClient : NexusApi
    {
        private SqlService _sql;


        protected override async Task<(T data, ResponseMetadata header)> Send<T>(HttpRequestMessage msg, CancellationToken token = default)
        {
            var (t, headers) = await base.Send<T>(msg, token);

            await _sql.SetNexusAPIKey(await ApiKey.GetKey(), headers.DailyRemaining, headers.HourlyRemaining);
            return (t, headers);
        }

        public TrackingClient(ApiKey apiKey, ILogger<NexusApi> logger, HttpClient client, ApplicationInfo appInfo, JsonSerializerOptions jsonOptions, SqlService sql) 
            : base(apiKey, logger, client, appInfo, jsonOptions)
        {
            _sql = sql;
        }
    }
}
