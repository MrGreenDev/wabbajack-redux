using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wabbajack.Common;
using Wabbajack.Downloaders;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.Networking.Http;
using Wabbajack.Networking.NexusApi;
using Wabbajack.Networking.NexusApi.Test.Helpers;
using Wabbajack.Networking.WabbajackClientApi;
using Wabbajack.Paths.IO;
using Wabbajack.VFS;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace Wabbajack.Installer.Test
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection service)
        {
            service.AddSingleton<TemporaryFileManager, TemporaryFileManager>();
            service.AddSingleton(new FileHashCache(KnownFolders.EntryPoint.Combine("hashcache.sqlite")));
            service.AddSingleton(new VFSCache(KnownFolders.EntryPoint.Combine("vfscache.sqlite")));
            service.AddSingleton<Context>();
            service.AddSingleton<HttpClient>();
            service.AddHttpDownloader();
            service.AddNexusApi();
            service.AddSingleton<ApiKey, StaticApiKey>(p => new StaticApiKey(Environment.GetEnvironmentVariable("NEXUS_API_KEY")!));
            service.AddSingleton<IRateLimiter>(new FixedSizeRateLimiter(2));
            service.AddSingleton<FileExtractor.FileExtractor>();
            service.AddSingleton(new JsonSerializerOptions());
            service.AddDTOSerializer();
            service.AddDTOConverters();
            service.AddDownloadDispatcher();
            service.AddStandardInstaller();
            service.AddSingleton<Client>();
            service.AddSingleton(new ApplicationInfo()
            {
                AppName = "Wabbajack.Networking.NexusApi.Test",
                AppVersion = new Version(1, 0)
            });
            service.AddSingleton<Configuration>();
        }
        
        public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
            loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true; }));
    }
}