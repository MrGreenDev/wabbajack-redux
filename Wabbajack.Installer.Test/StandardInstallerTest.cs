using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Xunit;

namespace Wabbajack.Installer.Test
{
    public class StandardInstallerTest
    {
        private readonly StandardInstaller _installer;
        private readonly AbsolutePath _modList;
        private readonly DTOSerializer _serializer;
        private readonly IServiceProvider _provider;
        private readonly TemporaryFileManager _manager;

        public StandardInstallerTest(IServiceProvider provider, DTOSerializer serializer, TemporaryFileManager manager)
        {
            _provider = provider;
            _serializer = serializer;
            _modList = "TestData/MO2AndSKSETest.wabbajack".ToRelativePath().RelativeTo(KnownFolders.EntryPoint);
            _manager = manager;
        }
        
        [Fact]
        public async Task CanLoadModlistDefinition()
        {
            var modlist = await StandardInstaller.LoadFromFile(_serializer, _modList);
            Assert.Equal("MO2AndSKSETest", modlist.Name);
        }

        [Fact]
        public async Task CanInstallAList()
        {
            var modlist = await StandardInstaller.LoadFromFile(_serializer, _modList);
            using var scope = _provider.CreateScope();
            var config = _provider.GetService<InstallerConfiguration>()!;
            await using var installFolder = _manager.CreateFolder();
            config.Install = installFolder;
            config.Downloads = config.Install.Combine("downloads");
            config.ModlistArchive = _modList;
            config.ModList = modlist;
            config.Game = modlist.GameType;

            var installer = _provider.GetService<StandardInstaller>();
            Assert.True(await installer.Begin(CancellationToken.None));

        }
    }
}