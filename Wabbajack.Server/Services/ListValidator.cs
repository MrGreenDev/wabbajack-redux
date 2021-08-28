﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.BuildServer;
using Wabbajack.Common;
using Wabbajack.Downloaders;
using Wabbajack.DTOs;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.DTOs.ServerResponses;
using Wabbajack.Networking.NexusApi;
using Wabbajack.Networking.NexusApi.DTOs;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Wabbajack.Server.DataLayer;
using Wabbajack.Server.DTOs;

namespace Wabbajack.Server.Services
{
    public class ListValidator : AbstractService<ListValidator, int>
    {
        private SqlService _sql;
        private DiscordWebHook _discord;
        private ArchiveMaintainer _archives;
        
        private AsyncLock _healLock = new();
        private readonly ParallelOptions _parallelOptions;
        private readonly DownloadDispatcher _dispatcher;

        public IEnumerable<(ModListSummary Summary, DetailedStatus Detailed)> Summaries => ValidationInfo.Values.Select(e => (e.Summary, e.Detailed));
        
        public ConcurrentDictionary<string, (ModListSummary Summary, DetailedStatus Detailed, TimeSpan ValidationTime)> ValidationInfo = new();
        private readonly NexusApi _nexusApi;


        public ListValidator(ILogger<ListValidator> logger, AppSettings settings, SqlService sql, DiscordWebHook discord, 
            ArchiveMaintainer archives, QuickSync quickSync, DownloadDispatcher dispatcher, ParallelOptions parallelOptions, NexusApi nexusApi) 
            : base(logger, settings, quickSync, TimeSpan.FromMinutes(5))
        {
            _sql = sql;
            _discord = discord;
            _archives = archives;
            _dispatcher = dispatcher;
            _parallelOptions = parallelOptions;
            _nexusApi = nexusApi;
        }

        public override async Task<int> Execute()
        {
            var token = CancellationToken.None;
            var data = await _sql.GetValidationData();
            _logger.LogInformation("Found {count} nexus files", data.NexusFiles.Count);
            
            var oldSummaries = Summaries;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var results = await data.ModLists.Where(m => !m.ForceDown)
                .PMap(_parallelOptions, async metadata =>
            {
                var timer = new Stopwatch();
                timer.Start();
                var oldSummary =
                    oldSummaries.FirstOrDefault(s => s.Summary.MachineURL == metadata.Links.MachineURL);

                var mainFile = _dispatcher.Parse(new Uri(metadata.Links.Download));
                var mainArchive = new Archive
                {
                    State = mainFile!,
                    Size = metadata.DownloadMetadata!.Size, 
                    Hash = metadata.DownloadMetadata!.Hash
                };
                bool mainFailed = false;

                try
                {
                    if (mainArchive.State is WabbajackCDN)
                    {
                        if (!await _dispatcher.Verify(mainArchive, token))
                        {
                            mainFailed = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    mainFailed = true;
                }

                var listArchives = await _sql.ModListArchives(metadata.Links.MachineURL);
                var archives = await listArchives.PMap(_parallelOptions, async archive =>
                {
                    if (mainFailed)
                        return (archive, ArchiveStatus.InValid);
                    
                    try
                    {
                        ReportStarting(archive.State.PrimaryKeyString);
                        if (timer.Elapsed > Delay)
                        {
                            return (archive, ArchiveStatus.InValid);
                        }
                        
                        var (_, result) = await ValidateArchive(data, archive, token);
                        if (result == ArchiveStatus.InValid)
                        {
                            if (data.Mirrors.TryGetValue(archive.Hash, out var done))
                                return (archive, done ? ArchiveStatus.Mirrored : ArchiveStatus.Updating);
                            if ((await data.AllowedMirrors.Value).TryGetValue(archive.Hash, out var reason))
                            {
                                await _sql.StartMirror((archive.Hash, reason));
                                return (archive, ArchiveStatus.Updating);
                            }
                            if (archive.State is Nexus)
                                return (archive, result);
                            return await TryToHeal(data, archive, metadata, token);
                        }


                        return (archive, result);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"During Validation of {archive.Hash} {archive.State.PrimaryKeyString}");
                        return (archive, ArchiveStatus.InValid);
                    }
                    finally
                    {
                        ReportEnding(archive.State.PrimaryKeyString);
                    }
                }).ToList();

                var failedCount = archives.Count(f => f.Item2 == ArchiveStatus.InValid || f.Item2 == ArchiveStatus.Updating);
                var passCount = archives.Count(f => f.Item2 == ArchiveStatus.Valid || f.Item2 == ArchiveStatus.Updated);
                var updatingCount = archives.Count(f => f.Item2 == ArchiveStatus.Updating);
                var mirroredCount = archives.Count(f => f.Item2 == ArchiveStatus.Mirrored);

                var summary =  new ModListSummary
                {
                    Failed = failedCount,
                    Passed = passCount,
                    Updating = updatingCount,
                    Mirrored = mirroredCount,
                    MachineURL = metadata.Links.MachineURL,
                    Name = metadata.Title,
                    ModListIsMissing = mainFailed
                };

                var detailed = new DetailedStatus
                {
                    Name = metadata.Title,
                    Checked = DateTime.UtcNow,
                    DownloadMetaData = metadata.DownloadMetadata,
                    HasFailures = failedCount > 0,
                    MachineName = metadata.Links.MachineURL,
                    Archives = archives.Select(a => new DetailedStatusItem
                    {
                        Archive = a.Item1, 
                        IsFailing = a.Item2 == ArchiveStatus.InValid,
                        ArchiveStatus = a.Item2
                    }).ToArray()
                };

                if (timer.Elapsed > Delay)
                {
                    await _discord.Send(Channel.Ham,
                        new DiscordMessage
                        {
                            Embeds = new[]
                            {
                                new DiscordEmbed
                                {
                                    Title =
                                        $"Failing {summary.Name} (`{summary.MachineURL}`) because the max validation time expired",
                                    Url = new Uri(
                                        $"https://build.wabbajack.org/lists/status/{summary.MachineURL}.html")
                                }
                            }
                        });
                }

                if (oldSummary != default && oldSummary.Summary.Failed != summary.Failed)
                {
                    _logger.Log(LogLevel.Information, $"Number of failures {oldSummary.Summary.Failed} -> {summary.Failed}");

                    if (summary.HasFailures)
                    {
                        await _discord.Send(Channel.Ham,
                            new DiscordMessage
                            {
                                Embeds = new[]
                                {
                                    new DiscordEmbed
                                    {
                                        Title =
                                            $"Number of failures in {summary.Name} (`{summary.MachineURL}`) was {oldSummary.Summary.Failed} is now {summary.Failed}",
                                        Url = new Uri(
                                            $"https://build.wabbajack.org/lists/status/{summary.MachineURL}.html")
                                    }
                                }
                            });
                    }
                    
                    if (!summary.HasFailures && oldSummary.Summary.HasFailures)
                    {
                        await _discord.Send(Channel.Ham,
                            new DiscordMessage
                            {
                                Embeds = new[]
                                {
                                    new DiscordEmbed
                                    {
                                        Title = $"{summary.Name} (`{summary.MachineURL}`) is now passing.",
                                        Url = new Uri(
                                            $"https://build.wabbajack.org/lists/status/{summary.MachineURL}.html")

                                    }
                                }
                            });
                    }

                }
                
                timer.Stop();
                

                
                ValidationInfo[summary.MachineURL] = (summary, detailed, timer.Elapsed);
                
                return (summary, detailed);
            }).ToList();
            
            stopwatch.Stop();
            _logger.LogInformation($"Finished Validation in {stopwatch.Elapsed}");

            return Summaries.Count(s => s.Summary.HasFailures);
        }


        private async Task<(Archive, ArchiveStatus)> TryToHeal(ValidationData data, Archive archive, ModlistMetadata modList, CancellationToken token)
        {
            try
            {
                using var _ = await _healLock.WaitAsync();
                var srcDownload =
                    await _sql.GetArchiveDownload(archive.State.PrimaryKeyString, archive.Hash, archive.Size);
                if (srcDownload == null || srcDownload.IsFailed == true)
                {
                    _logger.Log(LogLevel.Information,
                        $"Cannot heal {archive.State.PrimaryKeyString} Size: {archive.Size} Hash: {(long)archive.Hash} because it hasn't been previously successfully downloaded");
                    return (archive, ArchiveStatus.InValid);
                }


                var patches = await _sql.PatchesForSource(archive.Hash);
                foreach (var patch in patches)
                {
                    if (patch.Finished is null)
                        return (archive, ArchiveStatus.Updating);

                    if (patch.IsFailed == true)
                        return (archive, ArchiveStatus.InValid);

                    var (_, status) = await ValidateArchive(data, patch.Dest.Archive, token);
                    if (status == ArchiveStatus.Valid)
                        return (archive, ArchiveStatus.Updated);
                }


                var upgradeTime = DateTime.UtcNow;
                _logger.LogInformation(
                    $"Validator Finding Upgrade for {archive.Hash} {archive.State.PrimaryKeyString}");

                Func<Archive, Task<AbsolutePath>> resolver = async findIt =>
                {
                    _logger.LogInformation($"Quick find for {findIt.State.PrimaryKeyString}");
                    var foundArchive = await _sql.GetArchiveDownload(findIt.State.PrimaryKeyString);
                    if (foundArchive == null)
                    {
                        _logger.LogInformation($"No Quick find for {findIt.State.PrimaryKeyString}");
                        return default;
                    }

                    return _archives.TryGetPath(foundArchive.Archive.Hash, out var path) ? path : default;
                };
                
                var upgrade = await _dispatcher.FindUpgrade(archive, resolver);


                if (upgrade.Archive == default)
                {
                    _logger.Log(LogLevel.Information,
                        $"Cannot heal {archive.State.PrimaryKeyString} because an alternative wasn't found");
                    return (archive, ArchiveStatus.InValid);
                }

                _logger.LogInformation(
                    $"Upgrade {upgrade.Archive.State.PrimaryKeyString} found for {archive.State.PrimaryKeyString}");


                {
                }

                var found = await _sql.GetArchiveDownload(upgrade.Archive.State.PrimaryKeyString, upgrade.Archive.Hash,
                    upgrade.Archive.Size);
                Guid id;
                
                if (found == null)
                {
                    if (upgrade.NewFile.Path.FileExists())
                        await _archives.Ingest(upgrade.NewFile.Path);
                    id = await _sql.AddKnownDownload(upgrade.Archive, upgradeTime);
                }
                else
                {
                    id = found.Id;
                }

                var destDownload = await _sql.GetArchiveDownload(id);

                if (destDownload.Archive.Hash == srcDownload.Archive.Hash &&
                    destDownload.Archive.State.PrimaryKeyString == srcDownload.Archive.State.PrimaryKeyString)
                {
                    _logger.Log(LogLevel.Information, $"Can't heal because src and dest match");
                    return (archive, ArchiveStatus.InValid);
                }

                if (destDownload.Archive.Hash == default)
                {
                    _logger.Log(LogLevel.Information,
                        "Can't heal because we got back a default hash for the downloaded file");
                    return (archive, ArchiveStatus.InValid);
                }


                var existing = await _sql.FindPatch(srcDownload.Id, destDownload.Id);
                if (existing == null)
                {
                    if (await _sql.AddPatch(new Patch {Src = srcDownload, Dest = destDownload}))
                    {

                        _logger.Log(LogLevel.Information,
                            $"Enqueued Patch from {srcDownload.Archive.Hash} to {destDownload.Archive.Hash}");
                        await _discord.Send(Channel.Ham,
                            new DiscordMessage
                            {
                                Content =
                                    $"Enqueued Patch from {srcDownload.Archive.Hash} to {destDownload.Archive.Hash} to auto-heal `{modList.Links.MachineURL}`"
                            });
                    }
                }

                await upgrade.NewFile.DisposeAsync();

                _logger.LogInformation($"Patch in progress {archive.Hash} {archive.State.PrimaryKeyString}");
                return (archive, ArchiveStatus.Updating);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "During healing");
                return (archive, ArchiveStatus.InValid);
            }
        }

        private async Task<(Archive archive, ArchiveStatus)> ValidateArchive(ValidationData data, Archive archive, CancellationToken token)
        {
            switch (archive.State)
            {
                case GoogleDrive _:
                    // Disabled for now due to GDrive rate-limiting the build server
                    return (archive, ArchiveStatus.Valid);
                case Nexus nexusState when data.NexusFiles.TryGetValue(
                    (nexusState.Game.MetaData().NexusGameId, nexusState.ModID, nexusState.FileID), out var category):
                    return (archive, category != null ? ArchiveStatus.Valid : ArchiveStatus.InValid);
                case Nexus ns:
                    return (archive, await FastNexusModStats(ns, token));
                case Manual _:
                    return (archive, ArchiveStatus.Valid);
                case ModDB _:
                    return (archive, ArchiveStatus.Valid);
                case GameFileSource _:
                    return (archive, ArchiveStatus.Valid);
                case MediaFire _:
                    return (archive, ArchiveStatus.Valid);
                case DeprecatedLoversLab _:
                    return (archive, ArchiveStatus.InValid);
                default:
                {
                    if (data.ArchiveStatus.TryGetValue((archive.State.PrimaryKeyString, archive.Hash),
                        out bool isValid))
                    {
                        return isValid ? (archive, ArchiveStatus.Valid) : (archive, ArchiveStatus.InValid);
                    }

                    return (archive, ArchiveStatus.Valid);
                }
            }
        }
        
        public async Task<ArchiveStatus> FastNexusModStats(Nexus ns, CancellationToken token)
        {
            // Check if some other thread has added them
            var file = await _sql.GetModFile(ns.Game, ns.ModID, ns.FileID);

            var queryTime = DateTime.UtcNow;
            if (file == null)
            {
                try
                {
                    _logger.Log(LogLevel.Information, "Found missing Nexus file info {Game} {ModID} {FileID}", ns.Game, ns.ModID, ns.FileID);
                    try
                    {
                        var (file2, headers) = await _nexusApi.FileInfo(ns.Game.MetaData().NexusName, ns.ModID, ns.FileID, token);
                        file = file2;
                    }
                    catch
                    {
                        file = new ModFile {CategoryName = null};
                    }

                    try
                    {
                        await _sql.AddNexusModFile(ns.Game, ns.ModID, ns.FileID, queryTime, file);
                    }
                    catch (Exception)
                    {
                        // Could be a PK constraint failure
                    }
                }
                catch (Exception)
                {
                    return ArchiveStatus.InValid;
                }
            }

            return file?.CategoryName != null ? ArchiveStatus.Valid : ArchiveStatus.InValid;

        }
    }
}
