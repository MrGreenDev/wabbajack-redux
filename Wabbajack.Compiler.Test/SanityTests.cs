using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wabbajack.Common;
using System.Linq;
using Wabbajack.DTOs.Directives;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Xunit;

namespace Wabbajack.Compiler.Test
{
    public class CompilerSanityTests
    {
        private readonly ILogger<CompilerSanityTests> _logger;
        private readonly IServiceProvider _serviceProvider;

        public CompilerSanityTests(ILogger<CompilerSanityTests> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }
        [Fact]
        public async Task CanCompileDirectMatchFiles()
        {
            using var scope = _serviceProvider.CreateScope();
            var harness = scope.ServiceProvider.GetService<ModListHarness>();

            var mod = await harness.InstallMod(Ext.Zip,
                new Uri("https://authored-files.wabbajack.org/Tonal%20Architect_WJ_TEST_FILES.zip_9cb97a01-3354-4077-9e4a-7e808d47794f"));
            var modlist =  await harness.Compile();
            Assert.NotNull(modlist);
            Assert.Single(modlist.Archives);
            
            Assert.NotEmpty(modlist.Directives.Select(d => d.To).ToHashSet());
            Assert.Equal(4, modlist.Directives.Length);

            
            foreach (var directive in modlist.Directives.OfType<FromArchive>()) 
                Assert.Equal(modlist.Archives.First().Hash, directive.ArchiveHashPath.Hash);
            

            await harness.Install();

            foreach (var file in mod.FullPath.EnumerateFiles())
                harness.VerifyInstalledFile(file);
        }
    }
}