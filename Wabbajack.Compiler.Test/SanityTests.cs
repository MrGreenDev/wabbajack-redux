using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wabbajack.Common;
using System.Linq;
using Wabbajack.DTOs;
using Wabbajack.DTOs.Directives;
using Wabbajack.DTOs.Directives;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Xunit;

namespace Wabbajack.Compiler.Test
{
    public class CompilerSanityTests : IAsyncLifetime
    {
        private readonly ILogger<CompilerSanityTests> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScope _scope;
        private readonly ModListHarness _harness;
        private Mod _mod;
        private ModList? _modlist;

        public CompilerSanityTests(ILogger<CompilerSanityTests> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _scope = _serviceProvider.CreateScope();
            _harness = _scope.ServiceProvider.GetService<ModListHarness>()!;

        }
        
        
        public async Task InitializeAsync()
        {
            _mod = await _harness.InstallMod(Ext.Zip,
                new Uri("https://authored-files.wabbajack.org/Tonal%20Architect_WJ_TEST_FILES.zip_9cb97a01-3354-4077-9e4a-7e808d47794f"));
        }
        
        private async Task CompileAndValidate(int expectedDirectives)
        {
            _modlist = await _harness.Compile();
            Assert.NotNull(_modlist);
            Assert.Single(_modlist!.Archives);

            Assert.NotEmpty(_modlist.Directives.Select(d => d.To).ToHashSet());
            Assert.Equal(expectedDirectives, _modlist.Directives.Length);
        }
        
        private async Task InstallAndValidate()
        {
            await _harness.Install();

            foreach (var file in _mod.FullPath.EnumerateFiles())
                _harness.VerifyInstalledFile(file);
        }

        public async Task DisposeAsync()
        {
            
        }
        
        [Fact]
        public async Task CanCompileDirectMatchFiles()
        {
            await CompileAndValidate(4);

            foreach (var directive in _modlist!.Directives.OfType<FromArchive>()) 
                Assert.Equal(_modlist.Archives.First().Hash, directive.ArchiveHashPath.Hash);
            
            await InstallAndValidate();
        }
        
        [Fact]
        public async Task CanPatchFiles()
        {
            foreach (var file in _mod.FullPath.EnumerateFiles(Ext.Esp))
            {
                await using var fs = file.Open(FileMode.Open, FileAccess.Write);
                fs.Position = 42;
                fs.WriteByte(42);
            }
            
            await CompileAndValidate(4);
            
            Assert.Single(_modlist.Directives.OfType<PatchedFromArchive>());
            await InstallAndValidate();
        }


    }
}