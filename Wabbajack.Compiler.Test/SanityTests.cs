using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wabbajack.Common;
using Wabbajack.Paths;
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

            await harness.InstallMod(Ext.Zip,
                new Uri("https://authored-files.wabbajack.org/Tonal%20Architect_WJ_TEST_FILES.zip_9cb97a01-3354-4077-9e4a-7e808d47794f"));
            Assert.True(await harness.Compile());
        }
    }
}