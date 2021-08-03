using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wabbajack.Networking.NexusApi;
using Wabbajack.Paths.IO;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;
using Wabbajack.Networking.Http;

namespace Wabbajack.Downloaders.GoogleDrive.Test
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection service)
        {
            service.AddNexusApi();
            service.AddSingleton<HttpClient, HttpClient>();
            service.AddSingleton<TemporaryFileManager, TemporaryFileManager>();
            service.AddHttpDownloader();
            service.AddGoogleDriveDownloader();
            service.AddSingleton(new ApplicationInfo()
            {
                AppName = "Wabbajack.Networking.NexusApi.Test",
                AppVersion = new Version(1, 0)
            });
            service.AddSingleton(new JsonSerializerOptions());
        }
        
        public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
            loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true; }));
    }
}