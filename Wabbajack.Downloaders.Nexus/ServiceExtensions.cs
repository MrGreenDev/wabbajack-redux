using Microsoft.Extensions.DependencyInjection;
using Wabbajack.Downloaders.Interfaces;
using Wabbajack.DTOs.DownloadStates;

namespace Wabbajack.Downloaders
{
    public static class ServiceExtensions
    {
        public static void AddNexusDownloader(this IServiceCollection services)
        {
            services.AddSingleton<IDownloader, NexusDownloader>();
            services.AddSingleton<NexusDownloader, NexusDownloader>();
            services.AddSingleton<IDownloader<Nexus>, NexusDownloader>();
        }
    }
}