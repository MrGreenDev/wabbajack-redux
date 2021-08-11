using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            var mod = harness.AddMod();
            var path = await mod.AddData("foo.pez".ToRelativePath(), "Cheese for Everyone!");

            await harness.AddManualDownload(path);
            
            Assert.True(await harness.Compile());
        }
    }
}