using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using Wabbajack.Common;
using Wabbajack.Common.FileSignatures;
using Wabbajack.DTOs.Streams;
using Wabbajack.FileExtractor.ExtractedFiles;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Wabbajack.TaskTracking.Interfaces;

namespace Wabbajack.FileExtractor
{
    public class FileExtractor
    {
        private readonly ILogger<FileExtractor> _logger;
        private readonly IRateLimiter _limiter;
        private readonly TemporaryFileManager _manager;

        public FileExtractor(ILogger<FileExtractor> logger, IRateLimiter limiter, TemporaryFileManager manager)
        {
            _logger = logger;
            _limiter = limiter;
            _manager = manager;
        }
        
        public async Task<IDictionary<RelativePath,T>> GatheringExtractWith7Zip<T>(
            IStreamFactory sf, 
            FileType sig, 
            Predicate<RelativePath> shouldExtract, 
            Func<RelativePath,IExtractedFile,ValueTask<T>> mapfn,
            IReadOnlyCollection<RelativePath>? onlyFiles, 
            CancellationToken token)
        {
            TemporaryPath? tmpFile = null;
            await using var dest = _manager.CreateFolder();

            TemporaryPath? spoolFile = null;
            AbsolutePath source;

            try
            {
                if (sf.Name is AbsolutePath abs)
                {
                    source = abs;
                }
                else
                {
                    spoolFile = _manager.CreateFile(sf.Name.FileName.Extension);
                    await using var s = await sf.GetStream();
                    await spoolFile.Value.Path.WriteAllAsync(s, token);
                    source = spoolFile.Value.Path;
                }

                _logger.LogInformation("Extracting {source}", source.FileName);

                
                string initialPath = "";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    initialPath = @"Extractors\windows-x64\7z.exe";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    initialPath = @"Extractors\linux-x64\7zz";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    initialPath = @"Extractors\mac\7zz";

                var process = new ProcessHelper {Path = initialPath.ToRelativePath().RelativeTo(KnownFolders.EntryPoint),};

                if (onlyFiles != null)
                {
                    //It's stupid that we have to do this, but 7zip's file pattern matching isn't very fuzzy
                    IEnumerable<string> AllVariants(string input)
                    {
                        yield return $"\"{input}\"";
                        yield return $"\"\\{input}\"";
                    }

                    tmpFile = _manager.CreateFile();
                    await tmpFile.Value.Path.WriteAllLinesAsync(onlyFiles.SelectMany(f => AllVariants((string)f)), token);
                    process.Arguments = new object[]
                    {
                        "x", "-bsp1", "-y", $"-o\"{dest}\"", source, $"@\"{tmpFile.Value.ToString()}\"", "-mmt=off"
                    };
                }
                else
                {
                    process.Arguments = new object[] {"x", "-bsp1", "-y", $"-o\"{dest}\"", source, "-mmt=off"};
                }
                
                _logger.LogInformation("{prog} {args}", process.Path, process.Arguments);


                var result = process.Output.Where(d => d.Type == ProcessHelper.StreamType.Output)
                    .ForEachAsync(p =>
                    {
                        var (_, line) = p;
                        if (line == null)
                            return;

                        if (line.Length <= 4 || line[3] != '%') return;

                        int.TryParse(line[..3], out var percentInt);
                        //Utils.Status($"Extracting {(string)source.FileName} - {line.Trim()}",
                        //    Percent.FactoryPutInRange(percentInt / 100d));
                    }, token);

                var exitCode = await process.Start();


                /*
                if (exitCode != 0)
                {
                    Utils.ErrorThrow(new _7zipReturnError(exitCode, source, dest, ""));
                }
                else
                {
                    Utils.Status($"Extracting {source.FileName} - done", Percent.One, alsoLog: true);
                }*/

                var results = await dest.Path.EnumerateFiles()
                    .PMap(_limiter, async f =>
                    {
                        var path = f.RelativeTo(dest.Path);
                        if (!shouldExtract(path)) return ((RelativePath, T))default;
                        var file = new ExtractedNativeFile(f);
                        var mapResult = await mapfn(path, file);
                        f.Delete();
                        return (path, mapResult);
                    })
                    .Where(d => d.Item1 != default)
                    .ToDictionary(d => d.Item1, d => d.Item2);

                return results;
            }
            finally
            { 
                if (tmpFile != null)
                {
                    await tmpFile.Value.DisposeAsync();
                }

                if (spoolFile != null)
                {
                    await spoolFile.Value.DisposeAsync();
                }
            }
        }
    }
}