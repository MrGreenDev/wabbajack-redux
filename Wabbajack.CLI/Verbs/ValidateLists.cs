using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.CLI.Services;
using Wabbajack.Common;
using Wabbajack.Downloaders;
using Wabbajack.DTOs;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.DTOs.GitHub;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.DTOs.ServerResponses;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Installer;
using Wabbajack.Networking.WabbajackClientApi;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;

namespace Wabbajack.CLI.Verbs
{
    public class ValidateLists : IVerb
    {
        private readonly ILogger<ValidateLists> _logger;
        private readonly Client _wjClient;
        private readonly Networking.GitHub.Client _gitHubClient;
        private readonly TemporaryFileManager _temporaryFileManager;
        private readonly DownloadDispatcher _dispatcher;
        private readonly DTOSerializer _dtos;

        public ValidateLists(ILogger<ValidateLists> logger, Client wjClient, 
            Wabbajack.Networking.GitHub.Client gitHubClient, TemporaryFileManager temporaryFileManager,
            DownloadDispatcher dispatcher, DTOSerializer dtos)
        {
            _logger = logger;
            _wjClient = wjClient;
            _gitHubClient = gitHubClient;
            _temporaryFileManager = temporaryFileManager;
            _dispatcher = dispatcher;
            _dtos = dtos;
        }
        
        public Command MakeCommand()
        {
            var command = new Command("validate-lists");
            command.Add(new Option<List[]>(new[] { "-l", "-lists" }, "Lists of lists to validate") {IsRequired = true});
            command.Add(new Option<AbsolutePath>(new[] { "-a", "-archives" }, "Location to store archives (files are named as the hex version of their hashes)")
                {IsRequired = true});
            command.Description = "Gets a list of modlists, validates them and exports a result list";
            command.Handler = CommandHandler.Create(Run);
            return command;
        }
        
        public async Task<int> Run(List[] lists, AbsolutePath archives)
        {
            var archiveManager = new ArchiveManager(_logger, archives);
            var token = CancellationToken.None;
            
            foreach (var list in lists)
            {
                _logger.LogInformation("Loading list of lists: {list}", list);
                var listData = await _gitHubClient.GetData(list);
                foreach (var modList in listData.Lists)
                {
                    if (modList.ForceDown)
                    {
                        _logger.LogWarning("List is ForceDown, skipping");
                        continue;
                    }
                    using var scope = _logger.BeginScope("MachineURL: {machineURL}", modList.Links.MachineURL);
                    _logger.LogInformation("Verifying {machineURL} - {title}", modList.Links.MachineURL,  modList.Title);
                    await ValidateList(modList, archiveManager, CancellationToken.None);

                    _logger.LogInformation("Loading Modlist");
                    var modListData =
                        await StandardInstaller.LoadFromFile(_dtos, archiveManager.GetPath(modList.DownloadMetadata!.Hash));
                    
                    _logger.LogInformation("Verifying {count} archives", modListData.Archives.Length);

                    foreach (var archive in modListData.Archives)
                    {
                        (ArchiveStatus status, Archive newArchive) = await DownloadAndValidate(archive, archiveManager, token);
                    }
                }

            }
            
            return 0;
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
                    await _dispatcher.Download(archive, tempFile.Path, token);
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

        private async Task ValidateList(ModlistMetadata modList, ArchiveManager archiveManager, CancellationToken token)
        {
            if (archiveManager.HaveArchive(modList.DownloadMetadata!.Hash))
            {
                _logger.LogInformation("Previously downloaded {hash} not re-downloading", modList.Links.MachineURL);
            }
            else
            {
                _logger.LogInformation("Downloading {hash}", modList.Links.MachineURL);
                await DownloadWabbajackFile(modList, archiveManager, token);
            }
            
        }

        private async Task DownloadWabbajackFile(ModlistMetadata modList, ArchiveManager archiveManager,
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

        }
    }
}