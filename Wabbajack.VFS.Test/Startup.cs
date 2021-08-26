using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wabbajack.Common;
using Wabbajack.Paths.IO;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace Wabbajack.VFS.Test
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection service)
        {
            service.AddSingleton<TemporaryFileManager, TemporaryFileManager>();

            // Keep this fixed at 2 so that we can detect deadlocks in the VFS parallelOptions
            service.AddSingleton(new ParallelOptions {MaxDegreeOfParallelism = 2});
            service.AddSingleton(new FileHashCache(KnownFolders.EntryPoint.Combine("hashcache.sqlite")));
            service.AddSingleton(new VFSCache(KnownFolders.EntryPoint.Combine("vfscache.sqlite")));
            service.AddTransient<Context>();
            service.AddSingleton<FileExtractor.FileExtractor>();
        }

        public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor)
        {
            loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true; }));
        }
    }
}