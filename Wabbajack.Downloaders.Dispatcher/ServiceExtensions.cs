using Microsoft.Extensions.DependencyInjection;
using Wabbajack.Downloaders.Http;
using Wabbajack.DTOs.JsonConverters;

namespace Wabbajack.Downloaders
{
    public static class ServiceExtensions
    {
        public static void AddDownloadDispatcher(this IServiceCollection services)
        {
            services.AddGoogleDriveDownloader();
            services.AddNexusDownloader();
            services.AddHttpDownloader();
            services.AddWabbajackCDNDownloader();
            services.AddDTOSerializer();
            services.AddDTOConverters();
            services.AddSingleton<DownloadDispatcher>();
        }
    }
}