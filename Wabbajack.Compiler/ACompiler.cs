using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using Wabbajack.Common;
using Wabbajack.Compiler.CompilationSteps;
using Wabbajack.Downloaders;
using Wabbajack.DTOs;
using Wabbajack.DTOs.Directives;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Installer;
using Wabbajack.Networking.WabbajackClientApi;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Wabbajack.TaskTracking.Interfaces;
using Wabbajack.VFS;

namespace Wabbajack.Compiler
{
    public abstract class ACompiler<T>
        where T : ACompiler<T>
    {
        public List<IndexedArchive> IndexedArchives = new();

        public Dictionary<Hash, IEnumerable<VirtualFile>> IndexedFiles = new();

        public ModList ModList = new();
        public AbsolutePath ModListImage;
        private readonly ILogger<T> _logger;
        private readonly FileExtractor.FileExtractor _extractor;
        private readonly FileHashCache _hashCache;
        private readonly Context _vfs;
        private readonly TemporaryFileManager _manager;
        private readonly CompilerSettings _settings;
        private readonly AbsolutePath _stagingFolder;
        private readonly IRateLimiter _limiter;

        private ConcurrentDictionary<Directive, RawSourceFile> _sourceFileLinks;
        private readonly DownloadDispatcher _dispatcher;
        private readonly Client _wjClient;
        private readonly GameLocator _locator;

        public ACompiler(ILogger<T> logger, FileExtractor.FileExtractor extractor, FileHashCache hashCache, Context vfs, TemporaryFileManager manager, CompilerSettings settings,
            IRateLimiter limiter, DownloadDispatcher dispatcher, Client wjClient, GameLocator locator)
        {
            _logger = logger;
            _extractor = extractor;
            _hashCache = hashCache;
            _vfs = vfs;
            _manager = manager;
            _settings = settings;
            _stagingFolder = _manager.CreateFolder().Path;
            _limiter = limiter;
            _sourceFileLinks = new ConcurrentDictionary<Directive, RawSourceFile>();
            _dispatcher = dispatcher;
            _wjClient = wjClient;
            _locator = locator;

        }

        public CompilerSettings Settings { get; set; }

        public Dictionary<Game, HashSet<Hash>> GameHashes { get; set; } = new Dictionary<Game, HashSet<Hash>>();
        public Dictionary<Hash, Game[]> GamesWithHashes { get; set; } = new Dictionary<Hash, Game[]>();


        public bool IgnoreMissingFiles { get; set; }

        public List<Archive> SelectedArchives { get; protected set; } = new List<Archive>();
        public List<Directive> InstallDirectives { get; protected set; } = new List<Directive>();
        public List<RawSourceFile> AllFiles { get; protected set; } = new List<RawSourceFile>();

        public Dictionary<AbsolutePath, IndexedArchive> ArchivesByFullPath { get; set; } =
            new Dictionary<AbsolutePath, IndexedArchive>();
        
        internal RelativePath IncludeId()
        {
            return Guid.NewGuid().ToString().ToRelativePath();
        }

        internal async Task<RelativePath> IncludeFile(byte[] data)
        {
            var id = IncludeId();
            await _stagingFolder.Combine(id).WriteAllBytesAsync(data);
            return id;
        }

        internal AbsolutePath IncludeFile(out RelativePath id)
        {
            id = IncludeId();
            return _stagingFolder.Combine(id);
        }

        internal async Task<RelativePath> IncludeFile(string data)
        {
            var id = IncludeId();
            await _stagingFolder.Combine(id).WriteAllTextAsync(data);
            return id;
        }

        internal async Task<RelativePath> IncludeFile(Stream data, CancellationToken token)
        {
            var id = IncludeId();
            await _stagingFolder.Combine(id).WriteAllAsync(data, token);
            return id;
        }

        internal async Task<RelativePath> IncludeFile(AbsolutePath data, CancellationToken token)
        {
            await using var stream = data.Open(FileMode.Open);
            return await IncludeFile(stream, token);
        }


        internal async Task<(RelativePath, AbsolutePath)> IncludeString(string str)
        {
            var id = IncludeId();
            var fullPath = _stagingFolder.Combine(id);
            await fullPath.WriteAllTextAsync(str);
            return (id, fullPath);
        }

        public async Task<bool> GatherMetaData()
        {
            _logger.LogInformation("Getting meta data for {count} archives", SelectedArchives.Count);
            await SelectedArchives.PDo(_limiter, async a =>
            {
                await _dispatcher.FillInMetadata(a);
            });

            return true;
        }


        protected async Task IndexGameFileHashes()
        {
            if (_settings.UseGamePaths)
            {
                //taking the games in Settings.IncludedGames + currently compiling game so you can eg
                //include the stock game files if you are compiling for a VR game (ex: Skyrim + SkyrimVR)
                foreach (var ag in _settings.OtherGames.Append(_settings.Game).Distinct())
                {
                    try
                    {
                        if (!_locator.TryFindLocation(ag, out var path))
                        {
                            _logger.LogWarning("Game {game} was to be used in compilation but it is not installed", ag);
                            return;
                        }

                        var mainFile = ag.MetaData().MainExecutable!.Value.RelativeTo(path);

                        if (!mainFile.FileExists())
                        {
                            _logger.LogWarning("Main file {file} for {game} does not exist", mainFile, ag);
                        }

                        var versionInfo = FileVersionInfo.GetVersionInfo(mainFile.ToString());

                        var files = await _wjClient.GetGameArchives(ag, versionInfo.FileVersion ?? "0.0.0.0");
                        
                        _logger.LogInformation($"Including {files.Length} stock game files from {ag} as download sources");
                        GameHashes[ag] = files.Select(f => f.Hash).ToHashSet();

                        IndexedArchives.AddRange(files.Select(f =>
                        {
                            var state = (GameFileSource)f.State;
                            return new IndexedArchive(
                                _vfs.Index.ByRootPath[path.Combine(state.GameFile)])
                            {
                                Name = state.GameFile.ToString().Replace("/", "_").Replace("\\", "_")
                            };
                        }));
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "Unable to find existing game files for {game}, skipping.", ag);
                    }
                }

                GamesWithHashes = GameHashes.SelectMany(g => g.Value.Select(h => (g, h)))
                    .GroupBy(gh => gh.h)
                    .ToDictionary(gh => gh.Key, gh => gh.Select(p => p.g.Key).ToArray());
            }
        }

        protected async Task CleanInvalidArchivesAndFillState()
        {
            var remove = await IndexedArchives.PMap(_limiter, async a =>
            {
                try
                {
                    var resolved = await ResolveArchive(a);
                    if (resolved == null)
                    {
                        return null;
                    }

                    a.State = resolved.State;
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex.ToString(), "While resolving archive {archive}", a.Name);
                    return a;
                }
            }).ToHashSet(f => f != null);

            if (remove.Count == 0)
            {
                return;
            }

            _logger.LogWarning(
                "Removing {count} archives from the compilation state, this is probably not an issue but reference this if you have compilation failures", remove.Count);
            remove.Do(r => _logger.LogWarning("Resolution failed for: ({size} {hash}) {path}", r.File.Size, r.File.Hash, r.File.FullPath));
            IndexedArchives.RemoveAll(a => remove.Contains(a));
        }

        protected async Task InferMetas(CancellationToken token)
        {
            async Task<bool> HasInvalidMeta(AbsolutePath filename)
            {
                var metaName = filename.WithExtension(Ext.Meta);
                if (!metaName.FileExists())
                {
                    return true;
                }

                try
                {
                    var ini = metaName.LoadIniFile();
                    return await _dispatcher.ResolveArchive(ini["General"].ToDictionary(d => d.KeyName,  d => d.Value)) == null;
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, $"Exception while checking meta {filename}");
                    return false;
                }
            }

            var toFind = await (_settings.Downloads.EnumerateFiles()
                    .Where(f => f.Extension != Ext.Meta)
                    .PMap(_limiter, async f => await HasInvalidMeta(f) ? f : default))
                .Where(f => f.FileExists())
                .ToList();

            if (toFind.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Attempting to infer {count} metas from the server.", toFind.Count);

            await toFind.PDo(_limiter, async f =>
            {
                var vf = _vfs.Index.ByRootPath[f];

                var archives = await _wjClient.GetArchivesForHash(vf.Hash);

                Archive? a = null;
                foreach (var archive in archives)
                {
                    if (await _dispatcher.Verify(archive, token))
                    {
                        a = archive;
                        break;
                    }
                }

                if (a == null)
                {
                    await vf.AbsoluteName.WithExtension(Ext.Meta).WriteAllLinesAsync(
                        new string[] {"[General]",
                        "unknownArchive=true"}, token);
                    _logger.LogWarning("Could not infer meta for {archive} {hash}", f, vf.Hash);
                    return;
                }

                _logger.LogInformation($"Inferred .meta for {vf.FullPath.FileName}, writing to disk");
                await vf.AbsoluteName.WithExtension(Ext.Meta)
                    .WriteAllTextAsync(_dispatcher.MetaIniSection(a), token);
            });
        }

        protected async Task ExportModList()
        {
            Utils.Log($"Exporting ModList to {ModListOutputFile}");

            // Modify readme and ModList image to relative paths if they exist
            if (ModListImage.Exists)
            {
                ModList.Image = (RelativePath)"modlist-image.png";
            }

            await using (var of = await ModListOutputFolder.Combine("modlist").Create())
                ModList.ToJson(of);

            await ModListOutputFolder.Combine("sig")
                .WriteAllBytesAsync(((await ModListOutputFolder.Combine("modlist").FileHashAsync()) ?? Hash.Empty).ToArray());

            //await ClientAPI.SendModListDefinition(ModList);

            await ModListOutputFile.DeleteAsync();

            await using (var fs = await ModListOutputFile.Create())
            {
                using var za = new ZipArchive(fs, ZipArchiveMode.Create);

                await ModListOutputFolder.EnumerateFiles()
                    .DoProgress("Compressing ModList",
                        async f =>
                        {
                            var ze = za.CreateEntry((string)f.FileName);
                            await using var os = ze.Open();
                            await using var ins = await f.OpenRead();
                            await ins.CopyToAsync(os);
                        });

                // Copy in modimage
                if (ModListImage.Exists)
                {
                    var ze = za.CreateEntry((string)ModList.Image);
                    await using var os = ze.Open();
                    await using var ins = await ModListImage.OpenRead();
                    await ins.CopyToAsync(os);
                }
            }

            Utils.Log("Exporting Modlist metadata");
            var outputFileHash = await ModListOutputFile.FileHashAsync();
            if (outputFileHash == null)
            {
                Utils.Error("Unable to hash Modlist Output File");
                return;
            }
            
            var metadata = new DownloadMetadata
            {
                Size = ModListOutputFile.Size,
                Hash = outputFileHash.Value,
                NumberOfArchives = ModList.Archives.Count,
                SizeOfArchives = ModList.Archives.Sum(a => a.Size),
                NumberOfInstalledFiles = ModList.Directives.Count,
                SizeOfInstalledFiles = ModList.Directives.Sum(a => a.Size)
            };
            metadata.ToJson(ModListOutputFile + ".meta.json");

            Utils.Log("Removing ModList staging folder");
            await Utils.DeleteDirectory(ModListOutputFolder);
        }

        /// <summary>
        ///     Fills in the Patch fields in files that require them
        /// </summary>
        protected async Task BuildPatches()
        {
            Info("Gathering patch files");

            var toBuild = InstallDirectives.OfType<PatchedFromArchive>()
                .Where(p => p.Choices.Length > 0)
                .SelectMany(p => p.Choices.Select(c => new PatchedFromArchive
                {
                    To = p.To,
                    Hash = p.Hash,
                    ArchiveHashPath = c.MakeRelativePaths(),
                    FromFile = c,
                    Size = p.Size
                }))
                .ToArray();

            if (toBuild.Length == 0)
            {
                return;
            }

            // Extract all the source files
            var indexed = toBuild.GroupBy(f => VFS.Index.FileForArchiveHashPath(f.ArchiveHashPath))
                .ToDictionary(f => f.Key);
            await VFS.Extract(Queue, indexed.Keys.ToHashSet(),
                async (vf, sf) =>
                {
                    // For each, extract the destination
                    var matches = indexed[vf];
                    using var iqueue = new WorkQueue(1);
                    foreach (var match in matches)
                    {
                        var destFile = FindDestFile(match.To);
                        // Build the patch
                        await VFS.Extract(iqueue, new[] {destFile}.ToHashSet(),
                            async (destvf, destsfn) =>
                            {
                                Info($"Patching {match.To}");
                                Status($"Patching {match.To}");
                                await using var srcStream = await sf.GetStream();
                                await using var destStream = await destsfn.GetStream();
                                var patchSize =
                                    await Utils.CreatePatchCached(srcStream, vf.Hash, destStream, destvf.Hash);
                                Info($"Patch size {patchSize} for {match.To}");
                            });
                    }
                });

            // Load in the patches
            await InstallDirectives.OfType<PatchedFromArchive>()
                .Where(p => p.PatchID == default)
                .PMap(Queue, async pfa =>
                {
                    var patches = pfa.Choices
                        .Select(c => (Utils.TryGetPatch(c.Hash, pfa.Hash, out var data), data, c))
                        .ToArray();

                    // Pick the best patch
                    if (patches.All(p => p.Item1))
                    {
                        var (_, bytes, file) = IncludePatches.PickPatch(this, patches);
                        pfa.FromFile = file;
                        pfa.FromHash = file.Hash;
                        pfa.ArchiveHashPath = file.MakeRelativePaths();
                        pfa.PatchID = await IncludeFile(await bytes!.GetData());
                    }
                });

            var firstFailedPatch =
                InstallDirectives.OfType<PatchedFromArchive>().FirstOrDefault(f => f.PatchID == default);
            if (firstFailedPatch != null)
            {
                Utils.Log("Missing data from failed patch, starting data dump");
                Utils.Log($"Dest File: {firstFailedPatch.To}");
                Utils.Log($"Options ({firstFailedPatch.Choices.Length}:");
                foreach (var choice in firstFailedPatch.Choices)
                {
                    Utils.Log($"  {choice.FullPath}");
                }

                Error(
                    $"Missing patches after generation, this should not happen. First failure: {firstFailedPatch.FullPath}");
            }
        }

        private VirtualFile FindDestFile(RelativePath to)
        {
            var abs = to.RelativeTo(_settings.Source);
            if (abs.FileExists())
            {
                return _vfs.Index.ByRootPath[abs];
            }

            if (to.InFolder(Consts.BSACreationDir))
            {
                var bsaId = (RelativePath)((string)to).Split('\\')[1];
                var bsa = InstallDirectives.OfType<CreateBSA>().First(b => b.TempID == bsaId);
                var find = (RelativePath)Path.Combine(((string)to).Split('\\').Skip(2).ToArray());

                return _vfs.Index.ByRootPath[_settings.Source.Combine(bsa.To)].Children.First(c => c.RelativeName == find);
            }

            throw new ArgumentException($"Couldn't load data for {to}");
        }

        public void GenerateManifest()
        {
            var manifest = new Manifest(ModList);
            manifest.ToJson(ModListOutputFile + ".manifest.json");
        }

        public async Task GatherArchives()
        {
            _logger.LogInformation("Building a list of archives based on the files required");

            var hashes = InstallDirectives.OfType<FromArchive>()
                .Select(a => a.ArchiveHashPath.Hash)
                .Distinct();

            var archives = IndexedArchives.OrderByDescending(f => f.File.LastModified)
                .GroupBy(f => f.File.Hash)
                .ToDictionary(f => f.Key, f => f.First());

            SelectedArchives.Clear();
            SelectedArchives.AddRange(await hashes.PMap(_limiter, hash => ResolveArchive(hash, archives)).ToList());
        }

        public async Task<Archive> ResolveArchive(Hash hash, IDictionary<Hash, IndexedArchive> archives)
        {
            if (archives.TryGetValue(hash, out var found))
            {
                return await ResolveArchive(found);
            }

            throw new ArgumentException($"No match found for Archive sha: {hash.ToBase64()} this shouldn't happen");
        }

        public async Task<Archive?> ResolveArchive(IndexedArchive archive)
        {
            if (archive.IniData == null)
            {
                _logger.LogWarning(
                    "No download metadata found for {archive}, please use MO2 to query info or add a .meta file and try again.",
                    archive.Name);
                return null;
            }

            var state = await _dispatcher.ResolveArchive(archive.IniData!["General"].ToDictionary(d => d.KeyName, d => d.Value));

            if (state == null)
            {
                _logger.LogWarning("{archive} could not be handled by any of the downloaders", archive.Name);
                return null;
            }

            var result = new Archive
            {
                State = state,
                Name = archive.Name ?? "", 
                Hash = archive.File.Hash, 
                Size = (ulong)archive.File.Size
            };

            var downloader = _dispatcher.Downloader(result);
            await downloader.Prepare();

            var token = new CancellationTokenSource();
            token.CancelAfter(_settings.MaxVerificationTime);
            if (!await _dispatcher.Verify(result, token.Token))
            {
                _logger.LogWarning(
                    "Unable to resolve link for {archive}. If this is hosted on the Nexus the file may have been removed.", archive);
            }

            result.Meta = "[General]\n" + string.Join("\n", _dispatcher.MetaIni(result));
            return result;
        }

        public async Task<Directive> RunStack(IEnumerable<ICompilationStep> stack, RawSourceFile source)
        {
            foreach (var step in stack)
            {
                var result = await step.Run(source);
                if (result != null) return result;
            }

            throw new InvalidDataException("Data fell out of the compilation stack");
        }

        public abstract IEnumerable<ICompilationStep> GetStack();
        public abstract IEnumerable<ICompilationStep> MakeStack();

        public void PrintNoMatches(ICollection<NoMatch> noMatches)
        {
            const int max = 10;
            if (noMatches.Count > 0)
            {
                foreach (var file in noMatches)
                {
                    _logger.LogWarning("     {fileTo} - {fileReason}", file.To, file.Reason);
                }
            }
        }

        protected async Task InlineFiles(CancellationToken token)
        {
            var grouped = ModList.Directives.OfType<InlineFile>()
                .Where(f => f.SourceDataID == default)
                .GroupBy(f => _sourceFileLinks[f].File)
                .ToDictionary(k => k.Key);

            if (grouped.Count == 0) return;
            await _vfs.Extract(grouped.Keys.ToHashSet(), async (vf, sfn) =>
            {
                await using var stream = await sfn.GetStream();
                var id = await IncludeFile(stream);
                foreach (var file in grouped[vf])
                {
                    file.SourceDataID = id;
                }
            }, token);
        }


        public bool CheckForNoMatchExit(ICollection<NoMatch> noMatches)
        {
            if (noMatches.Count > 0)
            {
                _logger.LogCritical("Exiting due to no way to compile these files");
                return true;
            }

            return false;
        }
    }
}