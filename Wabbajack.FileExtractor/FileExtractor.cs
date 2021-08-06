using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using OMODFramework;
using Wabbajack.Common;
using Wabbajack.Common.FileSignatures;
using Wabbajack.Compression.BSA;
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

        public static readonly SignatureChecker ArchiveSigs = new(FileType.TES3,
            FileType.BSA,
            FileType.BA2,
            FileType.ZIP,
            //FileType.EXE,
            FileType.RAR_OLD,
            FileType.RAR_NEW,
            FileType._7Z);
        
        private static Extension OMODExtension = new(".omod");
        private static Extension FOMODExtension = new(".fomod");

        private static Extension BSAExtension = new(".bsa");
        
        public static readonly HashSet<Extension> ExtractableExtensions = new()
        {
            new(".bsa"),
            new(".ba2"),
            new(".7z"),
            new(".7zip"),
            new(".rar"),
            new(".zip"),
            OMODExtension,
            FOMODExtension
        };
        
        public async Task<IDictionary<RelativePath, T>> GatheringExtract<T>(
             IStreamFactory sFn,
            Predicate<RelativePath> shouldExtract, 
            Func<RelativePath, IExtractedFile, ValueTask<T>> mapfn,
             CancellationToken token,
            HashSet<RelativePath>? onlyFiles = null)
        {
            if (sFn is NativeFileStreamFactory)
            {
                _logger.LogInformation("Extracting {file}", sFn.Name);
            }
            await using var archive = await sFn.GetStream();
            var sig = await ArchiveSigs.MatchesAsync(archive);
            archive.Position = 0;
            
            

            IDictionary<RelativePath, T> results;

            switch (sig)
            {
                case FileType.RAR_OLD:
                case FileType.RAR_NEW:
                case FileType._7Z:
                case FileType.ZIP:
                {
                    if (sFn.Name.FileName.Extension == OMODExtension)
                    {
                        results = await GatheringExtractWithOMOD(archive, shouldExtract, mapfn, token);

                    }
                    else
                    {
                        await using var tempFolder = _manager.CreateFolder();
                        results = await GatheringExtractWith7Zip<T>(sFn, shouldExtract,
                            mapfn, onlyFiles, token);
                    }

                    break;
                }

                case FileType.BSA:
                case FileType.BA2:
                    results = await GatheringExtractWithBSA(sFn, (FileType)sig, shouldExtract, mapfn, token);
                    break;

                case FileType.TES3:
                    if (sFn.Name.FileName.Extension == BSAExtension)
                        results = await GatheringExtractWithBSA(sFn, (FileType)sig, shouldExtract, mapfn, token);
                    else
                        throw new Exception($"Invalid file format {sFn.Name}");
                    break;
                default:
                    throw new Exception($"Invalid file format {sFn.Name}");
            }

            if (onlyFiles != null && onlyFiles.Count != results.Count)
            {
                throw new Exception(
                    $"Sanity check error extracting {sFn.Name} - {results.Count} results, expected {onlyFiles.Count}");
            }
            return results;
        }
        
        private async Task<Dictionary<RelativePath,T>> GatheringExtractWithOMOD<T>
            (Stream archive, Predicate<RelativePath> shouldExtract, Func<RelativePath,IExtractedFile,ValueTask<T>> mapfn,
                CancellationToken token)
        {
            var tmpFile = _manager.CreateFile();
            await tmpFile.Path.WriteAllAsync(archive, CancellationToken.None);
            var dest = _manager.CreateFolder();

            using var omod = new OMOD(tmpFile.Path.ToString());

            var results = new Dictionary<RelativePath, T>();
            
            omod.ExtractFilesParallel(dest.Path.ToString(), 4, cancellationToken: token);
            if (omod.HasEntryFile(OMODEntryFileType.PluginsCRC))
                omod.ExtractFiles(false, dest.Path.ToString());

            var files = omod.GetDataFiles();
            if (omod.HasEntryFile(OMODEntryFileType.PluginsCRC))
                files.UnionWith(omod.GetPluginFiles());
            
            foreach (var compressedFile in files)
            {
                var abs = compressedFile.Name.ToRelativePath().RelativeTo(dest.Path);
                var rel = abs.RelativeTo(dest.Path); 
                if (!shouldExtract(rel)) continue;

                var result = await mapfn(rel, new ExtractedNativeFile(abs));
                results.Add(rel, result);
            }
            
            return results;
        }
        
        public static async Task<Dictionary<RelativePath,T>> GatheringExtractWithBSA<T>(IStreamFactory sFn, 
            FileType sig, 
            Predicate<RelativePath> shouldExtract, 
            Func<RelativePath,IExtractedFile,ValueTask<T>> mapFn, 
            CancellationToken token)
        {
            var archive = await BSADispatch.Open(sFn, sig);
            var results = new Dictionary<RelativePath, T>();
            foreach (var entry in archive.Files)
            {
                if (token.IsCancellationRequested) break;
                
                if (!shouldExtract(entry.Path))
                    continue;

                var result = await mapFn(entry.Path, new ExtractedMemoryFile(await entry.GetStreamFactory(token)));
                results.Add(entry.Path, result);
            }
            return results;
        }
        
        public async Task<IDictionary<RelativePath,T>> GatheringExtractWith7Zip<T>(
            IStreamFactory sf, 
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