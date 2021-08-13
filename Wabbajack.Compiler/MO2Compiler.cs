using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IniParser.Model;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using Wabbajack.Common;
using Wabbajack.Compiler.CompilationSteps;
using Wabbajack.Downloaders;
using Wabbajack.DTOs;
using Wabbajack.DTOs.Directives;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.Installer;
using Wabbajack.Networking.WabbajackClientApi;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Wabbajack.VFS;

namespace Wabbajack.Compiler
{
    public class MO2Compiler : ACompiler
    {
        private readonly MO2CompilerSettings _mo2Settings;

        public MO2Compiler(ILogger<MO2Compiler> logger, FileExtractor.FileExtractor extractor, FileHashCache hashCache, Context vfs, 
            TemporaryFileManager manager, MO2CompilerSettings settings, IRateLimiter limiter, DownloadDispatcher dispatcher, 
            Client wjClient, IGameLocator locator, DTOSerializer dtos, IBinaryPatchCache patchCache) : 
            base(logger, extractor, hashCache, vfs, manager, settings, limiter, dispatcher, wjClient, locator, dtos, patchCache)
        {
            _mo2Settings = settings;
        }
        
        public AbsolutePath MO2ModsFolder => _settings.Source.Combine(Consts.MO2ModFolderName);
        

        public IniData MO2Ini { get; }

        public AbsolutePath MO2ProfileDir => _settings.Source.Combine(Consts.MO2Profiles, _mo2Settings.Profile);

        public ConcurrentBag<Directive> ExtraFiles { get; private set; } = new();
        public Dictionary<AbsolutePath, IniData> ModInis { get; set; } = new();
        public static AbsolutePath GetTypicalDownloadsFolder(AbsolutePath mo2Folder)
        {
            return mo2Folder.Combine("downloads");
        }

        public async Task<bool> Begin(CancellationToken token)
        {
            await _wjClient.SendMetric("begin_compiling", _mo2Settings.Profile);
            
            var roots = new List<AbsolutePath> {_settings.Source, _settings.Downloads};
            roots.AddRange(_settings.OtherGames.Append(_settings.Game).Select(g => _locator.GameLocation(g)));
            
            await _vfs.AddRoots(roots, token);
            
            await InferMetas(token);

            await _vfs.AddRoot(_settings.Downloads, token);

            // Find all Downloads
            IndexedArchives = await _settings.Downloads.EnumerateFiles()
                .Where(f => f.WithExtension(Ext.Meta).FileExists())
                .PMap(_limiter,
                    async f => new IndexedArchive(_vfs.Index.ByRootPath[f])
                    {
                        Name = (string)f.FileName,
                        IniData = f.WithExtension(Ext.Meta).LoadIniFile(),
                        Meta = await f.WithExtension(Ext.Meta).ReadAllTextAsync()
                    }).ToList();


            await IndexGameFileHashes();

            IndexedArchives = IndexedArchives.DistinctBy(a => a.File.AbsoluteName).ToList();

            await CleanInvalidArchivesAndFillState();


            var mo2Files = _settings.Source.EnumerateFiles()
                .Where(p => p.FileExists())
                .Select(p => new RawSourceFile(_vfs.Index.ByRootPath[p], p.RelativeTo(_settings.Source)));

            // If Game Folder Files exists, ignore the game folder
            IndexedFiles = IndexedArchives.SelectMany(f => f.File.ThisAndAllChildren)
                .OrderBy(f => f.NestingFactor)
                .GroupBy(f => f.Hash)
                .ToDictionary(f => f.Key, f => f.AsEnumerable());
            
            AllFiles = mo2Files
                .DistinctBy(f => f.Path)
                .ToList();
            
            var dups = AllFiles.GroupBy(f => f.Path)
                .Where(fs => fs.Count() > 1)
                .ToList();

            if (dups.Count > 0)
            {
                _logger.LogInformation("Found {count} duplicates, exiting", dups.Count);
                return false;
            }
            
            ModInis = _settings.Source.Combine(Consts.MO2ModFolderName)
                .EnumerateDirectories()
                .Select(f =>
                {
                    var modName = f.FileName;
                    var metaPath = f.Combine("meta.ini");
                    return metaPath.FileExists() ? (mod_name: f, metaPath.LoadIniFile()) : default;
                })
                .Where(f => f.Item1 != default)
                .ToDictionary(f => f.mod_name, f => f.Item2);

            ArchivesByFullPath = IndexedArchives.ToDictionary(a => a.File.AbsoluteName);
            

            var stack = MakeStack();

            var results = await AllFiles.PMap(_limiter, f => RunStack(stack, f)).ToList();

            // Add the extra files that were generated by the stack

            results = results.Concat(ExtraFiles).ToList();

            var noMatch = results.OfType<NoMatch>().ToArray();
            PrintNoMatches(noMatch);
            if (CheckForNoMatchExit(noMatch))
            {
                return false;
            }

            foreach (var ignored in results.OfType<IgnoredDirectly>())
            {
                _logger.LogInformation("Ignored {to} because {reason}", ignored.To, ignored.Reason);
            }

            InstallDirectives = results.Where(i => i is not IgnoredDirectly).ToList();

            zEditIntegration.VerifyMerges(this);

            await BuildPatches(token);

            await GatherArchives();

            await GatherMetaData();

            ModList = new ModList
            {
                GameType = _settings.Game,
                WabbajackVersion = Consts.CurrentMinimumWabbajackVersion,
                Archives = SelectedArchives.ToArray(),
                Directives = InstallDirectives.ToArray(),
                Name = _settings.ModListName,
                Author = _settings.ModListAuthor,
                Description = _settings.ModListDescription,
                Readme = _settings.ModlistReadme,
                Image = ModListImage != default ? ModListImage.FileName : default,
                Website = _settings.ModListWebsite,
                Version = _settings.ModlistVersion,
                IsNSFW = _settings.ModlistIsNSFW
            };

            await InlineFiles(token);

            await RunValidation(ModList);
            
            await GenerateManifest();
            
            await ExportModList(token);
            
            ResetMembers();

            return true;
        }

        private async Task RunValidation(ModList modList)
        {
            var allowList = await _wjClient.LoadAllowList();
            foreach (var archive in modList.Archives)
            {
                if (!_dispatcher.IsAllowed(archive, allowList))
                {
                    _logger.LogCritical("Archive {name}, {primaryKeyString} is not allowed", archive.Name,
                        archive.State.PrimaryKeyString);
                    throw new CompilerException("Cannot download");
                }
            }
        }
        

        /// <summary>
        ///     Clear references to lists that hold a lot of data.
        /// </summary>
        private void ResetMembers()
        {
            AllFiles = new List<RawSourceFile>();
            InstallDirectives = new List<Directive>();
            SelectedArchives = new List<Archive>();
            ExtraFiles = new ConcurrentBag<Directive>();
        }

        public override IEnumerable<ICompilationStep> GetStack()
        {
            return MakeStack();
        }

        /// <summary>
        ///     Creates a execution stack. The stack should be passed into Run stack. Each function
        ///     in this stack will be run in-order and the first to return a non-null result will have its
        ///     result included into the pack
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<ICompilationStep> MakeStack()
        {
            _logger.LogInformation("Generating compilation stack");
            var steps = new List<ICompilationStep>
            {
                new IgnoreGameFilesIfGameFolderFilesExist(this),
                //new IncludeSteamWorkshopItems(this),
                new IgnoreSaveFiles(this),
                new IgnoreInPath(this, "logs".ToRelativePath()),
                new IgnoreInPath(this, "downloads".ToRelativePath()),
                new IgnoreInPath(this, "webcache".ToRelativePath()),
                new IgnoreInPath(this, "overwrite".ToRelativePath()),
                new IgnoreInPath(this, "crashDumps".ToRelativePath()),
                new IgnorePathContains(this, "temporary_logs"),
                new IgnorePathContains(this, "GPUCache"),
                new IgnorePathContains(this, "SSEEdit Cache"),
                new IgnoreOtherProfiles(this),
                new IgnoreDisabledMods(this),
                // TODO
                //new IgnoreTaggedFiles(this, Consts.WABBAJACK_IGNORE_FILES),
                //new IgnoreTaggedFolders(this,Consts.WABBAJACK_IGNORE),
                new IncludeThisProfile(this),
                // Ignore the ModOrganizer.ini file it contains info created by MO2 on startup
                new IncludeStubbedConfigFiles(this),
                new IgnoreInPath(this, Consts.GameFolderFilesDir.Combine("Data")),
                new IgnoreInPath(this, Consts.GameFolderFilesDir.Combine("Papyrus Compiler")),
                new IgnoreInPath(this, Consts.GameFolderFilesDir.Combine("Skyrim")),
                new IgnoreRegex(this, Consts.GameFolderFilesDir + "\\\\.*\\.bsa"),
                new IncludeRegex(this, "^[^\\\\]*\\.bat$"),
                new IncludeModIniData(this),
                new DirectMatch(this),
                new IncludeTaggedMods(this, Consts.WABBAJACK_INCLUDE),
                // TODO: rework tagged files
                // new IncludeTaggedFolders(this, Consts.WABBAJACK_INCLUDE),
                new IgnoreExtension(this, Ext.Pyc),
                new IgnoreExtension(this, Ext.Log),
                new DeconstructBSAs(
                    this), // Deconstruct BSAs before building patches so we don't generate massive patch files
                
                //new MatchSimilarTextures(this),
                new IncludePatches(this),
                new IncludeDummyESPs(this),

                // There are some types of files that will error the compilation, because they're created on-the-fly via tools
                // so if we don't have a match by this point, just drop them.
                new IgnoreExtension(this, Ext.Html),                
                // Don't know why, but this seems to get copied around a bit
                new IgnoreFilename(this, "HavokBehaviorPostProcess.exe".ToRelativePath()),
                // Theme file MO2 downloads somehow
                new IncludeRegex(this, "splash\\.png"),
                // File to force MO2 into portable mode
                new IgnoreFilename(this, "portable.txt".ToRelativePath()),
                new IgnoreExtension(this, Ext.Bin),
                new IgnoreFilename(this, ".refcache".ToRelativePath()),
                //Include custom categories  
                new IncludeRegex(this, "categories.dat$"),

                new IncludeAllConfigs(this),
                // TODO
                //new zEditIntegration.IncludeZEditPatches(this),
                new IncludeTaggedMods(this, Consts.WABBAJACK_NOMATCH_INCLUDE),
                // TODO
                //new IncludeTaggedFolders(this,Consts.WABBAJACK_NOMATCH_INCLUDE),
                //new IncludeTaggedFiles(this,Consts.WABBAJACK_NOMATCH_INCLUDE_FILES),
                new IncludeRegex(this, ".*\\.txt"),
                new IgnorePathContains(this,@"\Edit Scripts\Export\"),
                new IgnoreExtension(this, new Extension(".CACHE")),
                new DropAll(this)
            };

            //if (DisableTextureResizing)
            //    steps = steps.Where(s => !(s is MatchSimilarTextures)).ToList();

            return steps;
        }


    }
}