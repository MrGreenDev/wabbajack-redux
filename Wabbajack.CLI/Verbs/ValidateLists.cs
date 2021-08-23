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
using Wabbajack.DTOs.GitHub;
using Wabbajack.Hashing.xxHash64;
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

        public ValidateLists(ILogger<ValidateLists> logger, Client wjClient, 
            Wabbajack.Networking.GitHub.Client gitHubClient, TemporaryFileManager temporaryFileManager,
            DownloadDispatcher dispatcher)
        {
            _logger = logger;
            _wjClient = wjClient;
            _gitHubClient = gitHubClient;
            _temporaryFileManager = temporaryFileManager;
            _dispatcher = dispatcher;
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
                }

            }
            
            return 0;
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