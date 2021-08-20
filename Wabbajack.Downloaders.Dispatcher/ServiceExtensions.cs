using Microsoft.Extensions.DependencyInjection;
using Wabbajack.Downloaders.Http;
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
                .AddNexusDownloader()
                .AddWabbajackCDNDownloader()
                .AddSingleton<DownloadDispatcher>();
        }
    }
}