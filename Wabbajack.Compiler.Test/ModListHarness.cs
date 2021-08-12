using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wabbajack.Common;
using Wabbajack.Downloaders;
using Wabbajack.DTOs;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;

namespace Wabbajack.Compiler.Test
{
    public class ModListHarness
    {
        private readonly ILogger<ModListHarness> _logger;
        private readonly TemporaryFileManager _manager;
        private readonly AbsolutePath _source;
        private readonly AbsolutePath _downloadPath;
        private readonly AbsolutePath _installLocation;
        private readonly AbsolutePath _modsFolder;
        private readonly Dictionary<RelativePath,Mod> _mods;
        public readonly FileExtractor.FileExtractor _fileExtractor;
        private readonly string _profileName;
        private readonly IServiceProvider _serviceProvider;
        private readonly TemporaryPath _outputFolder;
        private readonly DownloadDispatcher _downloadDispatcher;

        public ModListHarness(ILogger<ModListHarness> logger, TemporaryFileManager manager, FileExtractor.FileExtractor fileExtractor, IServiceProvider serviceProvider,
            DownloadDispatcher downloadDispatcher)
        {
            _logger = logger;
            _manager = manager;
            _source = _manager.CreateFolder();
            _profileName = Guid.NewGuid().ToString();
            _downloadPath = _manager.CreateFolder();
            _installLocation = _manager.CreateFolder();
            _outputFolder = _manager.CreateFolder();
            _modsFolder = _source.Combine(Consts.MO2ModFolderName);
            _mods = new Dictionary<RelativePath, Mod>();
            _fileExtractor = fileExtractor;
            _serviceProvider = serviceProvider;
            _downloadDispatcher = downloadDispatcher;
        }

        public Mod AddMod(string? name = null)
        {
            name ??= Guid.NewGuid().ToString();
            var mod = new Mod(name.ToRelativePath(), _modsFolder.Combine(name), this);
            return mod;
        }

        public async Task<bool> Compile()
        {
            using var scope = _serviceProvider.CreateScope();
            var settings = scope.ServiceProvider.GetService<MO2CompilerSettings>();
            settings.Downloads = _downloadPath;
            settings.Game = Game.SkyrimSpecialEdition;
            settings.Source = _source;
            settings.ModListName = _profileName;
            settings.OutputFile = _outputFolder.Path.Combine(_profileName + ".wabbajack");

            var compiler = scope.ServiceProvider.GetService<MO2Compiler>();
            return await compiler.Begin(CancellationToken.None);
        }

        public async Task AddManualDownload(AbsolutePath path)
        {
            var toPath = path.FileName.RelativeTo(_downloadPath);
            await path.CopyToAsync(toPath, true, CancellationToken.None);

            await toPath.WithExtension(Ext.Meta)
                .WriteAllLinesAsync(new[] { "[General]", $"manualURL={path.FileName}" }, CancellationToken.None);
        }

        public async Task<Mod> InstallMod(Extension ext, Uri uri)
        {
            var state = _downloadDispatcher.Parse(uri);
            var file = (Guid.NewGuid() + ext.ToString()).ToRelativePath();
            var archive = new Archive { State = state!, Name = file.ToString() };
            _logger.LogInformation("Downloading: {uri}", uri);
            await _downloadDispatcher.Download(archive, file.RelativeTo(_downloadPath), CancellationToken.None);
            await file.WithExtension(Ext.Meta).RelativeTo(_downloadPath)
                .WriteAllTextAsync(_downloadDispatcher.MetaIniSection(archive), CancellationToken.None);
            
            var mod = AddMod(file.WithoutExtension().ToString());
            
            _logger.LogInformation("Extracting: {file}", file);
            await mod.AddFromArchive(file.RelativeTo(_downloadPath));
            return mod;
        }
    }

    public record Mod(RelativePath Name, AbsolutePath FullPath, ModListHarness Harness)
    {
        public async Task<AbsolutePath> AddFile(AbsolutePath src)
        {
            var dest = FullPath.Combine(src.FileName);
            await src.CopyToAsync(dest, true, CancellationToken.None);
            return dest;
        }
        
        public async Task AddFromArchive(AbsolutePath src)
        {
            var dest = FullPath.Combine(src.FileName);
            await Harness._fileExtractor.ExtractAll(src, dest, CancellationToken.None);
        }

        public async Task<AbsolutePath> AddData(RelativePath path, string data)
        {
            var fullPath = FullPath.Combine(path);
            fullPath.Parent.CreateDirectory();
            await fullPath.WriteAllTextAsync(data);
            return fullPath;
        }
    }
}