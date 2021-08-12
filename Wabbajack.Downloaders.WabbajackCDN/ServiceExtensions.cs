using Microsoft.Extensions.DependencyInjection;
using Wabbajack.Downloaders.Interfaces;
using Wabbajack.DTOs.DownloadStates;

namespace Wabbajack.Downloaders
{
    public static class ServiceExtensions
    {
        public static void AddWabbajackCDNDownloader(this IServiceCollection services)
        {
            services.AddSingleton<IDownloader, WabbajackCDNDownloader>();
            services.AddSingleton<WabbajackCDNDownloader, WabbajackCDNDownloader>();
            services.AddSingleton<IDownloader<WabbajackCDN>, WabbajackCDNDownloader>();
        }
    }
}