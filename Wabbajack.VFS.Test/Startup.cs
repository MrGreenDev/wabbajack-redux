using System;
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
            service.AddSingleton<IRateLimiter>(new StaticRateLimiter(32));//Environment.ProcessorCount));
            service.AddSingleton(new FileHashCache(KnownFolders.EntryPoint.Combine("hashcache.sqlite")));
            service.AddSingleton<Context>();
            service.AddSingleton<FileExtractor.FileExtractor>();
            service.AddSingleton(new VFSCache(KnownFolders.EntryPoint.Combine("vfscache.sqlite")));
        }
        
        public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
            loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true; }));
    }
}