using Microsoft.Extensions.DependencyInjection;
using Wabbajack.Downloaders.Interfaces;

namespace Wabbajack.Downloaders.Http
{
    public static class ServiceExtensions
    {
        public static void AddHttpDownloader(this IServiceCollection services)
        {
            services.AddSingleton<IDownloader, HttpDownloader>();
            services.AddSingleton<HttpDownloader, HttpDownloader>();
            services.AddSingleton<IDownloader<DTOs.DownloadStates.Http>, HttpDownloader>();
        }
    }
}