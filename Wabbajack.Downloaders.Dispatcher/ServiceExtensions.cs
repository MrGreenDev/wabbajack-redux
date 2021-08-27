using Microsoft.Extensions.DependencyInjection;
using Wabbajack.Downloaders.Http;
using Wabbajack.Downloaders.ModDB;
using Wabbajack.DTOs.JsonConverters;

namespace Wabbajack.Downloaders
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddDownloadDispatcher(this IServiceCollection services)
        {
            return services
                .AddDTOConverters()
                .AddDTOSerializer()
                .AddGoogleDriveDownloader()
                .AddHttpDownloader()
                .AddModDBDownloader()
                .AddNexusDownloader()
                .AddWabbajackCDNDownloader()
                .AddSingleton<DownloadDispatcher>();
        }
    }
}