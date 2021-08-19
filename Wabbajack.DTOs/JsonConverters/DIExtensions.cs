using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Wabbajack.Paths;

namespace Wabbajack.DTOs.JsonConverters
{
    public static class DIExtensions
    {
        public static void AddDTOConverters(this IServiceCollection services)
        {
            Wabbajack_DTOs_DownloadStates_IDownloadStateConverter.ConfigureServices(services);
            Wabbajack_DTOs_DirectiveConverter.ConfigureServices(services);
            Wabbajack_DTOs_BSA_ArchiveStates_IArchiveConverter.ConfigureServices(services);
            Wabbajack_DTOs_BSA_FileStates_AFileConverter.ConfigureServices(services);

            services.AddSingleton<JsonConverter, HashJsonConverter>();
            services.AddSingleton<JsonConverter, HashRelativePathConverter>();
            services.AddSingleton<JsonConverter, PHashConverter>();
            services.AddSingleton<JsonConverter, RelativePathConverter>();
            services.AddSingleton<JsonConverter, AbsolutePathConverter>();
            services.AddSingleton<JsonConverter, VersionConverter>();
        }

        public static void AddDTOSerializer(this IServiceCollection services)
        {
            services.AddSingleton<DTOSerializer>();
        }
    }
}