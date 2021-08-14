﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.BuildServer;
using Wabbajack.Common;
using Wabbajack.Server.DataLayer;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Wabbajack.Server.Services
{
    public class NonNexusDownloadValidator : AbstractService<NonNexusDownloadValidator, int>
    {
        private SqlService _sql;

        public NonNexusDownloadValidator(ILogger<NonNexusDownloadValidator> logger, AppSettings settings, SqlService sql, QuickSync quickSync)
            : base(logger, settings, quickSync, TimeSpan.FromHours(2))
        {
            _sql = sql;
        }

        public override async Task<int> Execute()
        {
            var archives = await _sql.GetNonNexusModlistArchives();
            _logger.Log(LogLevel.Information, $"Validating {archives.Count} non-Nexus archives");
            using var queue = new WorkQueue(10);
            await DownloadDispatcher.PrepareAll(archives.Select(a => a.State));

            var random = new Random();
            var results = await archives.PMap(queue, async archive =>
            {
                try
                {
                    await Task.Delay(random.Next(1000, 5000));
                    
                    var token = new CancellationTokenSource();
                    token.CancelAfter(TimeSpan.FromMinutes(10));
                    
                    ReportStarting(archive.State.PrimaryKeyString);
                    bool isValid = false;
                    switch (archive.State)
                    {
                        //case WabbajackCDNDownloader.State _: 
                        //case GoogleDriveDownloader.State _: // Let's try validating Google again 2/10/2021
                        case GameFileSourceDownloader.State _:
                            isValid = true;
                            break;
                        case ManualDownloader.State _:
                        case ModDBDownloader.State _:
                        case HTTPDownloader.State h when h.Url.StartsWith("https://wabbajack"):
                            isValid = true;
                            break;
                        default:
                            isValid = await archive.State.Verify(archive, token.Token);
                            break;
                    }
                    return (Archive: archive, IsValid: isValid);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Warning, $"Error for {archive.Name} {archive.State.PrimaryKeyString} {ex}");
                    return (Archive: archive, IsValid: false);
                }
                finally
                {
                    ReportEnding(archive.State.PrimaryKeyString);
                }

            });

            await _sql.UpdateNonNexusModlistArchivesStatus(results);
            var failed = results.Count(r => !r.IsValid);
            var passed = results.Count() - failed;
            foreach(var (archive, _) in results.Where(f => !f.IsValid))
                _logger.Log(LogLevel.Warning, $"Validation failed for {archive.Name} from {archive.State.PrimaryKeyString}");
            
            _logger.Log(LogLevel.Information, $"Non-nexus validation completed {failed} out of {passed} failed");
            return failed;
        }
    }
}
