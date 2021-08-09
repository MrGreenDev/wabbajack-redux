using Microsoft.Extensions.DependencyInjection;
using Wabbajack.Downloaders.Http;

namespace Wabbajack.Downloaders
{
    public static class ServiceExtensions
    {
        public static void AddDownloadDispatcher(this IServiceCollection services)
        {
            services.AddGoogleDriveDownloader();
            services.AddNexusDownloader();
            services.AddHttpDownloader();
            services.AddSingleton<DownloadDispatcher>();
        }
    }
}