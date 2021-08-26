using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.Downloaders;
using Wabbajack.DTOs;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;

namespace Wabbajack.CLI.Verbs
{
    public class DownloadCef : IVerb
    {
        private readonly ILogger<DownloadCef> _logger;
        private readonly DownloadDispatcher _dispatcher;
        private readonly FileExtractor.FileExtractor _fileExtractor;

        public DownloadCef(ILogger<DownloadCef> logger, DownloadDispatcher dispatcher, FileExtractor.FileExtractor fileExtractor)
        {
            _logger = logger;
            _dispatcher = dispatcher;
            _fileExtractor = fileExtractor;
        }

        public Command MakeCommand()
        {
            var command = new Command("download-cef");
            command.Add(new Option<AbsolutePath>(new[] { "-f", "-folder" }, "Path to Wabbajack"));
            command.Description = "Downloads CEF into this folder";
            command.Handler = CommandHandler.Create(Run);
            return command;
        }


        public async Task<int> Run(AbsolutePath folder)
        {
            if (folder == default) folder = KnownFolders.EntryPoint;

            var cefNet = folder.Combine("CefNet.dll");
            if (!cefNet.FileExists())
            {
                _logger.LogError("Cannot find CefNet.dll in {folder}", folder);
                return 1;
            }

            var version = Version.Parse(FileVersionInfo.GetVersionInfo(cefNet.ToString()).FileVersion!);
            var downloadVersion = $"{version.Major}.{version.Minor}";
            var runtime = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;
            if (folder.Combine("libcef.dll").FileExists())
            {
                _logger.LogInformation("Not downloading, cef already exists");
                return 0;
            }
            
            _logger.LogInformation("Downloading Cef version {version} for {runtime}", downloadVersion, runtime);

            var fileUri =
                new Uri(
                    $"https://github.com/wabbajack-tools/cef-builds/releases/download/{downloadVersion}/{runtime}.7z");

            var parsed = _dispatcher.Parse(fileUri);
            var tempFile = folder.Combine($"{runtime}.7z");
            await _dispatcher.Download(new Archive { State = parsed! }, tempFile, CancellationToken.None);
            
            _logger.LogInformation("Extracting {file}", tempFile);
            await _fileExtractor.ExtractAll(tempFile, folder, CancellationToken.None);
            tempFile.Delete();
            
            return 0;
        }
    }
}