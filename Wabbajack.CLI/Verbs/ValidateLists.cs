using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Microsoft.Extensions.Logging;
using Wabbajack.CLI.Services;
using Wabbajack.Common;
using Wabbajack.Downloaders;
using Wabbajack.DTOs;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.DTOs.GitHub;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.DTOs.ModListValidation;
using Wabbajack.DTOs.ServerResponses;
using Wabbajack.DTOs.Validation;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Installer;
using Wabbajack.Networking.WabbajackClientApi;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Wabbajack.Server.Lib.DTOs;
using Wabbajack.Server.Lib.TokenProviders;

namespace Wabbajack.CLI.Verbs
{
    public class ValidateLists : IVerb
    {
        private static Uri MirrorPrefix = new Uri("https://mirror.wabbajack.org");
        
        private readonly ILogger<ValidateLists> _logger;
        private readonly Client _wjClient;
        private readonly Networking.GitHub.Client _gitHubClient;
        private readonly TemporaryFileManager _temporaryFileManager;
        private readonly DownloadDispatcher _dispatcher;
        private readonly DTOSerializer _dtos;
        private readonly ParallelOptions _parallelOptions;
        private readonly IFtpSiteCredentials _ftpSiteCredentials;

        public ValidateLists(ILogger<ValidateLists> logger, Client wjClient, 
            Wabbajack.Networking.GitHub.Client gitHubClient, TemporaryFileManager temporaryFileManager,
            DownloadDispatcher dispatcher, DTOSerializer dtos, ParallelOptions parallelOptions,
            IFtpSiteCredentials ftpSiteCredentials)
        {
            _logger = logger;
            _wjClient = wjClient;
            _gitHubClient = gitHubClient;
            _temporaryFileManager = temporaryFileManager;
            _dispatcher = dispatcher;
            _dtos = dtos;
            _parallelOptions = parallelOptions;
            _ftpSiteCredentials = ftpSiteCredentials;
        }
        
        public Command MakeCommand()
        {
            var command = new Command("validate-lists");
            command.Add(new Option<List[]>(new[] { "-l", "-lists" }, "Lists of lists to validate") {IsRequired = true});
            command.Add(new Option<AbsolutePath>(new [] {"-r", "--reports"}, "Location to store validation report outputs"));
            command.Add(new Option<AbsolutePath>(new[] { "-a", "-archives" }, "Location to store archives (files are named as the hex version of their hashes)")
                {IsRequired = true});
            command.Description = "Gets a list of modlists, validates them and exports a result list";
            command.Handler = CommandHandler.Create(Run);
            return command;
        }
        
        public async Task<int> Run(List[] lists, AbsolutePath archives, AbsolutePath reports)
        {
            reports.CreateDirectory();
            var archiveManager = new ArchiveManager(_logger, archives);
            var token = CancellationToken.None;

            _logger.LogInformation("Scanning for existing patches/mirrors");
            var mirroredFiles = await AllMirroredFiles(token);
            var patchFiles = await AllPatchFiles(token);

            _logger.LogInformation("Loading Mirror Allow List");
            var mirrorAllowList = await _wjClient.LoadMirrorAllowList();

            var validationCache = new LazyCache<string, Archive, (ArchiveStatus Status, Archive archive)>
                (x => x.State.PrimaryKeyString, archive => DownloadAndValidate(archive, archiveManager, token));

            var mirrorCache = new LazyCache<string, Archive, (ArchiveStatus Status, Archive archive)>
                (x => x.State.PrimaryKeyString, archive => AttemptToMirrorArchive(archive, archiveManager, mirrorAllowList, mirroredFiles, token));
            
            foreach (var list in lists.Take(1))
            {
                _logger.LogInformation("Loading list of lists: {list}", list);
                var listData = await _gitHubClient.GetData(list);
                var stopWatch = Stopwatch.StartNew();
                var validatedLists = await listData.Lists.Skip(2).Take(1).PMapAll(async modList =>
                {
                    if (modList.ForceDown)
                    {
                        _logger.LogWarning("List is ForceDown, skipping");
                        return new ValidatedModList { Status = ListStatus.ForcedDown };
                    }

                    using var scope = _logger.BeginScope("MachineURL: {machineURL}", modList.Links.MachineURL);
                    _logger.LogInformation("Verifying {machineURL} - {title}", modList.Links.MachineURL, modList.Title);
                    await DownloadModList(modList, archiveManager, CancellationToken.None);

                    _logger.LogInformation("Loading Modlist");
                    var modListData =
                        await StandardInstaller.LoadFromFile(_dtos,
                            archiveManager.GetPath(modList.DownloadMetadata!.Hash));

                    _logger.LogInformation("Verifying {count} archives", modListData.Archives.Length);

                    var archives = await modListData.Archives.PMapAll(async archive =>
                    {
                        //var result = await DownloadAndValidate(archive, archiveManager, token);
                        var result = await validationCache.Get(archive);

                        if (result.Status == ArchiveStatus.InValid)
                        {
                            result = await  mirrorCache.Get(archive);
                        }

                        return new ValidatedArchive
                        {
                            Original = archive,
                            Status = result.Status,
                            PatchedFrom = result.Status is ArchiveStatus.Mirrored or ArchiveStatus.Updated
                                ? result.archive
                                : null
                        };
                    }).ToArray();
                    return new ValidatedModList
                    {
                        ModListHash = modList.DownloadMetadata.Hash,
                        MachineURL = modList.Links.MachineURL,
                        Archives = archives
                    };
                }).ToArray();

                var allArchives = validatedLists.SelectMany(l => l.Archives).ToList();
                _logger.LogInformation("Validated {count} lists in {elapsed}", validatedLists.Length, stopWatch.Elapsed);
                _logger.LogInformation(" - {count} Valid", allArchives.Count(a => a.Status is ArchiveStatus.Valid));
                _logger.LogInformation(" - {count} Invalid", allArchives.Count(a => a.Status is ArchiveStatus.InValid));
                _logger.LogInformation(" - {count} Mirrored", allArchives.Count(a => a.Status is ArchiveStatus.Mirrored));
                _logger.LogInformation(" - {count} Updated", allArchives.Count(a => a.Status is ArchiveStatus.Updated));

                foreach (var invalid in allArchives.Where(a => a.Status is ArchiveStatus.InValid))
                {
                    _logger.LogInformation("-- Invalid : {primaryKeyString}", invalid.Original.State.PrimaryKeyString);
                }

                await ExportReports(reports, validatedLists);

            }
            
            return 0;
        }

        private async Task ExportReports(AbsolutePath reports, ValidatedModList[] validatedLists)
        {
            foreach (var validatedList in validatedLists)
            {
                var baseFile = reports.Combine(validatedList.MachineURL);
                await using var jsonFile = baseFile.WithExtension(Ext.Json)
                    .Open(FileMode.Create, FileAccess.Write, FileShare.None);
                await _dtos.Serialize(validatedList, jsonFile, true);
            }


            var summaries = validatedLists.Select(l => new ModListSummary
            {
                Failed = l.Archives.Count(f => f.Status == ArchiveStatus.InValid),
                Mirrored = l.Archives.Count(f => f.Status == ArchiveStatus.Mirrored),
                Passed = l.Archives.Count(f => f.Status == ArchiveStatus.Valid),
                MachineURL = l.MachineURL,
                Name = l.Name,
                Updating = 0
            }).ToArray();
            
            await using var summaryFile = reports.Combine("modListSummary.json")
                .Open(FileMode.Create, FileAccess.Write, FileShare.None);
            await _dtos.Serialize(summaries, summaryFile, true);
        }

        private async Task<(ArchiveStatus Status, Archive archive)> AttemptToMirrorArchive(Archive archive,
            ArchiveManager archiveManager, ServerAllowList mirrorAllowList, HashSet<Hash> previouslyMirrored, 
            CancellationToken token)
        {
            // Are we allowed to mirror the file?
            if (!_dispatcher.Matches(archive, mirrorAllowList)) return (ArchiveStatus.InValid, archive);

            var mirroredArchive = new Archive
            {
                Name = archive.Name,
                Size = archive.Size,
                Hash = archive.Hash,
                State = new WabbajackCDN
                {
                    Url = new Uri($"{MirrorPrefix}{archive.Hash}")
                }
            };

            // If it's already mirrored, we can exit
            if (previouslyMirrored.Contains(archive.Hash)) return (ArchiveStatus.Mirrored, mirroredArchive);
            
            // We need to mirror the file, but do we have a copy to mirror?
            if (!archiveManager.HaveArchive(archive.Hash)) return (ArchiveStatus.InValid, mirroredArchive);

            var srcPath = archiveManager.GetPath(archive.Hash);
            var definition = await _wjClient.GenerateFileDefinition(srcPath);

            using (var client = await GetMirrorFtpClient(token))
            {
                await client.CreateDirectoryAsync($"{definition.Hash.ToHex()}", token);
                await client.CreateDirectoryAsync($"{definition.Hash.ToHex()}/parts", token);
            }

            string MakePath(long idx)
            {
                return $"{definition!.Hash.ToHex()}/parts/{idx}";
            }

            await definition.Parts.PDo(_parallelOptions, async part =>
            {
                _logger.LogInformation("Uploading mirror part of {name} {hash} ({index}/{length})", archive.Name, archive.Hash, part.Index, definition.Parts.Length);

                var buffer = new byte[part.Size];
                await using (var fs = srcPath.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fs.Position = part.Offset;
                    await fs.ReadAsync(buffer, token);
                }
                
                await CircuitBreaker.WithAutoRetryAllAsync(_logger, async () =>{
                    using var client = await GetMirrorFtpClient(token);
                    var name = MakePath(part.Index);
                    await client.UploadAsync(new MemoryStream(buffer), name, token: token);
                });

            });

            await CircuitBreaker.WithAutoRetryAllAsync(_logger, async () =>
            {
                using var client = await GetMirrorFtpClient(token);
                _logger.LogInformation($"Finishing mirror upload");


                await using var ms = new MemoryStream();
                await using (var gz = new GZipStream(ms, CompressionLevel.Optimal, true))
                {
                    await _dtos.Serialize(definition, gz);
                }

                ms.Position = 0;
                var remoteName = $"{definition.Hash.ToHex()}/definition.json.gz";
                await client.UploadAsync(ms, remoteName, token: token);
            });

            
            return (ArchiveStatus.Mirrored, mirroredArchive);
        }

        private async Task<(ArchiveStatus, Archive)> DownloadAndValidate(Archive archive, ArchiveManager archiveManager, CancellationToken token)
        {
            switch (archive.State)
            {
                case GameFileSource:
                    return (ArchiveStatus.Valid, archive);
                case Manual:
                    return (ArchiveStatus.Valid, archive);
            }
            
            
            if (!archiveManager.HaveArchive(archive.Hash) && archive.State is not Nexus or WabbajackCDN)
            {
                _logger.LogInformation("Downloading {name} {hash}", archive.Name, archive.Hash);

                try
                {
                    await using var tempFile = _temporaryFileManager.CreateFile();
                    var hash = await _dispatcher.Download(archive, tempFile.Path, token);
                    if (hash != archive.Hash)
                        return (ArchiveStatus.InValid, archive);
                    await archiveManager.Ingest(tempFile.Path, token);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Downloading {primaryKeyString}", archive.State.PrimaryKeyString);
                    return (ArchiveStatus.InValid, archive);
                }
            }

            try
            {
                var valid = await _dispatcher.Verify(archive, token);
                if (valid)
                    return (ArchiveStatus.Valid, archive);
                
                _logger.LogWarning("Archive {primaryKeyString} is invalid", archive.State.PrimaryKeyString);
                return (ArchiveStatus.InValid, archive);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "While verifying {primaryKeyString}", archive.State.PrimaryKeyString);
                return (ArchiveStatus.InValid, archive);
            }
        }

        private async Task<Hash> DownloadModList(ModlistMetadata modList, ArchiveManager archiveManager, CancellationToken token)
        {
            if (archiveManager.HaveArchive(modList.DownloadMetadata!.Hash))
            {
                _logger.LogInformation("Previously downloaded {hash} not re-downloading", modList.Links.MachineURL);
                return modList.DownloadMetadata!.Hash;
            }
            else
            {
                _logger.LogInformation("Downloading {hash}", modList.Links.MachineURL);
                return await DownloadWabbajackFile(modList, archiveManager, token);
            }
            
            
        }

        private async Task<Hash> DownloadWabbajackFile(ModlistMetadata modList, ArchiveManager archiveManager,
            CancellationToken token)
        {
            var state = _dispatcher.Parse(new Uri(modList.Links.Download));
            if (state == null)
                _logger.LogCritical("Can't download {url}", modList.Links.Download);
            
            var archive = new Archive
            {
                State = state!,
                Size = modList.DownloadMetadata!.Size,
                Hash = modList.DownloadMetadata.Hash
            };

            await using var tempFile = _temporaryFileManager.CreateFile(Ext.Wabbajack);
            _logger.LogInformation("Downloading {primaryKeyString}", state.PrimaryKeyString);
            var hash = await _dispatcher.Download(archive, tempFile.Path, token);
            
            if (hash != modList.DownloadMetadata.Hash) {
                _logger.LogCritical("Downloaded modlist was {actual} expected {expected}", hash, modList.DownloadMetadata.Hash);
                throw new Exception();
            }

            _logger.LogInformation("Archiving {hash}", hash);
            await archiveManager.Ingest(tempFile.Path, token);
            return hash;
        }

        public async ValueTask<HashSet<Hash>> AllMirroredFiles(CancellationToken token)
        {
            using var client = await GetMirrorFtpClient(token);
            var files = await client.GetListingAsync(token);
            var parsed = files.TryKeep(f => (Hash.TryGetFromHex(f.Name, out var hash), hash)).ToHashSet();
            return parsed;
        }
        
        public async ValueTask<HashSet<(Hash, Hash)>> AllPatchFiles(CancellationToken token)
        {
            using var client = await GetPatchesFtpClient(token);
            var files = await client.GetListingAsync(token);
            var parsed = files.TryKeep(f =>
            {
                var parts = f.Name.Split("_");
                return (parts.Length == 2, parts);
            })
                .TryKeep(p => (Hash.TryGetFromHex(p[0], out var fromHash) & 
                                     Hash.TryGetFromHex(p[1], out var toHash), 
                    (fromHash, toHash)))
                .ToHashSet();
            return parsed;
        }

        private async Task<FtpClient> GetMirrorFtpClient(CancellationToken token)
        {
            var client = await (await _ftpSiteCredentials.Get())![StorageSpace.Mirrors].GetClient(_logger);
            await client.ConnectAsync(token);
            return client;
        }
        
        private async Task<FtpClient> GetPatchesFtpClient(CancellationToken token)
        {
            var client = await (await _ftpSiteCredentials.Get())![StorageSpace.Patches].GetClient(_logger);
            await client.ConnectAsync(token);
            return client;
        }
    }
}