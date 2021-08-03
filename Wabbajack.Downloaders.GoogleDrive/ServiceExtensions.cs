using Microsoft.Extensions.DependencyInjection;
using Wabbajack.Downloaders.GoogleDrive;
using Wabbajack.Downloaders.Interfaces;
using Wabbajack.DTOs.DownloadStates;

namespace Wabbajack.Downloaders
{
    public static class ServiceExtensions
    {
        public static void AddGoogleDriveDownloader(this IServiceCollection services)
        {
            services.AddSingleton<IDownloader, GoogleDriveDownloader>();
            services.AddSingleton<GoogleDriveDownloader, GoogleDriveDownloader>();
            services.AddSingleton<IDownloader<DTOs.DownloadStates.GoogleDrive>, GoogleDriveDownloader>();
        }
    }
}