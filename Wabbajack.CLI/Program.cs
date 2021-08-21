using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wabbajack.CLI.TypeConverters;
using Wabbajack.CLI.Verbs;
using Wabbajack.Common;
using Wabbajack.Downloaders;
using Wabbajack.Networking.Http;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Networking.NexusApi;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Wabbajack.VFS;

namespace Wabbajack.CLI
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            TypeDescriptor.AddAttributes(typeof(AbsolutePath),
                new TypeConverterAttribute(typeof(AbsolutePathTypeConverter)));

            var host = Host.CreateDefaultBuilder(Array.Empty<string>())
                .ConfigureServices((host, services) =>
                {
                    services.AddSingleton(new ApplicationInfo
                    {
                        AppName = "Wabbajack.Networking.NexusApi.Test",
                        AppVersion = new Version(1, 0)
                    });

                    services.AddSingleton(new JsonSerializerOptions());
                    services.AddSingleton<ApiKey, StaticApiKey>(p =>
                        new StaticApiKey(Environment.GetEnvironmentVariable("NEXUS_API_KEY")!));
                    services.AddNexusApi();
                    services.AddSingleton<HttpClient, HttpClient>();
                    services.AddSingleton<IHttpDownloader, SingleThreadedDownloader>();
                    services.AddDownloadDispatcher();
                    services.AddSingleton<IConsole, SystemConsole>();
                    services.AddSingleton<CommandLineBuilder, CommandLineBuilder>();
                    services.AddSingleton<TemporaryFileManager>();
                    services.AddSingleton<FileExtractor.FileExtractor>();
                    services.AddSingleton(new VFSCache(KnownFolders.EntryPoint.Combine("vfscache.sqlite")));
                    services.AddSingleton(new FileHashCache(KnownFolders.EntryPoint.Combine("filehashpath.sqlite")));
                    services.AddSingleton<IRateLimiter>(new FixedSizeRateLimiter(Environment.ProcessorCount));

                    services.AddTransient<Context>();
                    services.AddSingleton<IVerb, HashFile>();
                    services.AddSingleton<IVerb, VFSIndexFolder>();
                    services.AddSingleton<IVerb, Encrypt>();
                }).Build();

            var service = host.Services.GetService<CommandLineBuilder>();
            return await service!.Run(args);
        }
    }
}