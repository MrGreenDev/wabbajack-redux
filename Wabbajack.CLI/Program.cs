using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wabbajack.CLI.TypeConverters;
using Wabbajack.CLI.Verbs;
using Wabbajack.Downloaders;
using Wabbajack.Networking.Http;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Networking.NexusApi;
using Wabbajack.Paths;

namespace Wabbajack.CLI
{
    class Program
    {
        public static Type[] AllVerbs =>
            typeof(Program).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.IsAssignableTo(typeof(IVerb))).ToArray();
        
        static async Task<int> Main(string[] args)
        {
            TypeDescriptor.AddAttributes(typeof(AbsolutePath), new TypeConverterAttribute(typeof(AbsolutePathTypeConverter)));
            
            var host = Host.CreateDefaultBuilder(Array.Empty<string>())
                .ConfigureServices((host, services) =>
                {
                    foreach (var verb in AllVerbs)
                    {
                        services.AddSingleton(typeof(IVerb), verb);
                    }
                    
                    services.AddSingleton(new ApplicationInfo()
                    {
                        AppName = "Wabbajack.Networking.NexusApi.Test",
                        AppVersion = new Version(1, 0)
                    });

                    services.AddSingleton(new JsonSerializerOptions());
                    services.AddSingleton<ApiKey, StaticApiKey>(p => new StaticApiKey(Environment.GetEnvironmentVariable("NEXUS_API_KEY")!));
                    services.AddNexusApi();
                    services.AddSingleton<HttpClient, HttpClient>();
                    services.AddSingleton<IHttpDownloader, SingleThreadedDownloader>();
                    services.AddDownloadDispatcher();
                    services.AddSingleton<IConsole, SystemConsole>();
                    services.AddSingleton<CommandLineBuilder, CommandLineBuilder>();

                }).Build();

            var service = host.Services.GetService<CommandLineBuilder>();
            return await service!.Run(args);

        }
    }
}