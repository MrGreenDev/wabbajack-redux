﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Wabbajack.BuildServer;
using Wabbajack.Common;
using Wabbajack.Lib.NexusApi;
using Wabbajack.Server.DataLayer;
using Wabbajack.Server.DTOs;

namespace Wabbajack.Server.Services
{
    public class NexusPoll
    {
        private SqlService _sql;
        private AppSettings _settings;
        private GlobalInformation _globalInformation;
        private ILogger<NexusPoll> _logger;
        private NexusKeyMaintainance _keys;

        public NexusPoll(ILogger<NexusPoll> logger, AppSettings settings, SqlService service, GlobalInformation globalInformation, NexusKeyMaintainance keys)
        {
            _sql = service;
            _settings = settings;
            _globalInformation = globalInformation;
            _logger = logger;
            _keys = keys;
        }

        public async Task UpdateNexusCacheRSS()
        {
            using var _ = _logger.BeginScope("Nexus Update via RSS");
            _logger.Log(LogLevel.Information, "Starting");

            var results = await NexusUpdatesFeeds.GetUpdates();
            long updated = 0;
            foreach (var result in results)
            {
                try
                {
                    var purgedMods =
                        await _sql.DeleteNexusModFilesUpdatedBeforeDate(result.Game, result.ModId, result.TimeStamp);
                    var purgedFiles =
                        await _sql.DeleteNexusModInfosUpdatedBeforeDate(result.Game, result.ModId, result.TimeStamp);

                    var totalPurged = purgedFiles + purgedMods;
                    if (totalPurged > 0)
                        _logger.Log(LogLevel.Information, $"Purged {totalPurged} cache items {result.Game} {result.ModId} {result.TimeStamp}");

                    updated += totalPurged;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed Nexus update for {result.Game} - {result.ModId} - {result.TimeStamp}");
                }

            }

            if (updated > 0) 
                _logger.Log(LogLevel.Information, $"RSS Purged {updated} nexus cache entries");

            _globalInformation.LastNexusSyncUTC = DateTime.UtcNow;
        }

        public async Task UpdateNexusCacheAPI()
        {
            using var _ = _logger.BeginScope("Nexus Update via API");
            _logger.Log(LogLevel.Information, "Starting Nexus Update via API");
            var api = await _keys.GetClient();
            
            var gameTasks = GameRegistry.Games.Values
                .Where(game => game.NexusName != null)
                .Select(async game =>
                {
                    var mods = await api.Get<List<NexusUpdateEntry>>(
                        $"https://api.nexusmods.com/v1/games/{game.NexusName}/mods/updated.json?period=1m");

                    return (game, mods);
                })
                .Select(async rTask =>
                {
                    var (game, mods) = await rTask;
                    return mods.Select(mod => new { game = game, mod = mod });
                }).ToList();

            _logger.Log(LogLevel.Information, $"Getting update list for {gameTasks.Count} games");

            var purge = (await Task.WhenAll(gameTasks))
                .SelectMany(i => i)
                .ToList();

            _logger.Log(LogLevel.Information, $"Found {purge.Count} updated mods in the last month");
            using var queue = new WorkQueue();
            var collected = purge.Select(d =>
            {
                var a = d.mod.LatestFileUpdate.AsUnixTime();
                // Mod activity could hide files
                var b = d.mod.LastestModActivity.AsUnixTime();

                return new {Game = d.game.Game, Date = (a > b) ? a : b, ModId = d.mod.ModId};
            });
                    
            var purged = await collected.PMap(queue, async t =>
            {
                long purgeCount = 0;
                purgeCount += await _sql.DeleteNexusModInfosUpdatedBeforeDate(t.Game, t.ModId, t.Date);
                purgeCount += await _sql.DeleteNexusModFilesUpdatedBeforeDate(t.Game, t.ModId, t.Date);
                return purgeCount;
            });

            _logger.Log(LogLevel.Information, $"Purged {purged.Sum()} cache entries");
            _globalInformation.LastNexusSyncUTC = DateTime.UtcNow;

        }

        public void Start()
        {
            if (!_settings.RunBackEndJobs) return;
            /*
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await UpdateNexusCacheRSS();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error polling from Nexus");
                    }
                    await Task.Delay(_globalInformation.NexusRSSPollRate);
                }
            });
*/
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await UpdateNexusCacheAPI();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting API feed from Nexus");
                    }

                    await Task.Delay(_globalInformation.NexusAPIPollRate);
                }
            });
        }
    }
    
    public static class NexusPollExtensions 
    {
        public static void UseNexusPoll(this IApplicationBuilder b)
        {
            var poll = (NexusPoll)b.ApplicationServices.GetService(typeof(NexusPoll));
            poll.Start();
        }
    }
}
