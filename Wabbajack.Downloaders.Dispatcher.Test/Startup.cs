using System;
using System.Net.Http;
using System.Reflection.Emit;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wabbajack.Common;
using Wabbajack.Networking.Http;
using Wabbajack.Networking.NexusApi;
using Wabbajack.Networking.NexusApi.Test.Helpers;
using Wabbajack.Networking.WabbajackClientApi;
using Wabbajack.Paths.IO;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace Wabbajack.Downloaders.Dispatcher.Test
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection service)
        {
            service.AddNexusApi();
            service.AddSingleton<HttpClient, HttpClient>();
            service.AddSingleton<TemporaryFileManager, TemporaryFileManager>();
            service.AddSingleton<Client>();
            service.AddSingleton<Configuration>();
            service.AddDownloadDispatcher();
            service.AddHttpDownloader();
            service.AddSingleton<ApiKey, StaticApiKey>(p =>
                new StaticApiKey(Environment.GetEnvironmentVariable("NEXUS_API_KEY")!));
            service.AddSingleton<IRateLimiter>(new FixedSizeRateLimiter(Environment.ProcessorCount));
            service.AddSingleton(new ApplicationInfo
            {
                AppName = "Wabbajack.Networking.NexusApi.Test",
                AppVersion = new Version(1, 0)
            });
            service.AddSingleton(new JsonSerializerOptions());
        }

        public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor)
        {
            loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true; }));
        }
    }
}