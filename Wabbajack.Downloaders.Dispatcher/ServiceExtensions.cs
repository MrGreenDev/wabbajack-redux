using Microsoft.Extensions.DependencyInjection;
using Wabbajack.Downloaders.GoogleDrive;
using Wabbajack.Downloaders.Interfaces;
using Wabbajack.DTOs.DownloadStates;

namespace Wabbajack.Downloaders
{
    public static class ServiceExtensions
    {
        public static void AddDownloadDispatcher(this IServiceCollection services)
        {
            services.AddGoogleDriveDownloader();
            services.AddNexusDownloader();
            services.AddSingleton<DownloadDispatcher>();
        }
    }
}