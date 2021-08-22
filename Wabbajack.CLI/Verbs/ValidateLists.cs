using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

        public ValidateLists(ILogger<ValidateLists> logger, Client wjClient, Wabbajack.Networking.GitHub.Client gitHubClient)
        {
            _logger = logger;
            _wjClient = wjClient;
            _gitHubClient = gitHubClient;
        }
        
        public Command MakeCommand()
        {
            var command = new Command("validate-lists");
            command.Add(new Option<List>(new[] { "-l", "-list" }, "Lists of lists to validate"));
            command.Description = "Gets a list of modlists, validates them and exports a result list";
            command.Handler = CommandHandler.Create(Run);
            return command;
        }
        
        public async Task<int> Run(IEnumerable<List> lists)
        {
            foreach (var list in lists)
            {
                _logger.LogInformation("Loading list of lists: {list}", list);
                var listData = await _gitHubClient.GetData(list);
                foreach (var modList in listData.Lists)
                {
                    _logger.LogInformation("Verifying {machineURL} - {title}", modList.Links.MachineURL,  modList.Title);
                    
                }

            }
            
            return 0;
        }
    }
}