using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wabbajack.Networking.NexusApi;
using Wabbajack.Networking.NexusApi.Test.Helpers;
using Wabbajack.Paths.IO;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;
using Wabbajack.Networking.Http;

namespace Wabbajack.Downloaders.Nexus.Test
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection service)
        {
            service.AddNexusApi();
            service.AddSingleton<ApiKey, StaticApiKey>();
            service.AddSingleton<HttpClient, HttpClient>();
            service.AddSingleton<TemporaryFileManager, TemporaryFileManager>();
            service.AddNexusDownloader();
            service.AddHttpDownloader();
            service.AddSingleton(new ApplicationInfo()
            {
                AppName = "Wabbajack.Networking.NexusApi.Test",
                AppVersion = new Version(1, 0)
            });
            service.AddSingleton(new JsonSerializerOptions());
            service.AddSingleton<ApiKey, StaticApiKey>(p => new StaticApiKey(Environment.GetEnvironmentVariable("NEXUS_API_KEY")!));
        }
        
        public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
            loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true; }));
    }
}