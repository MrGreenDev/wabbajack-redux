using Microsoft.Extensions.DependencyInjection;
using Wabbajack.Common;
using Wabbajack.Downloaders;
using Wabbajack.Installer;
using Wabbajack.Paths.IO;
using Wabbajack.VFS;

namespace Wabbajack.Services.OSIntegrated
{
    public static class ServiceExtensions
    {
        /// <summary>
        /// Adds variants of services that integrate into global OS services. These are not testing
        /// variants or services that require Environment variables. These are the "full fat" services.
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddOSIntegrated(this IServiceCollection service)
        {
            service.AddSingleton(new FileHashCache(KnownFolders.AppDataLocal.Combine("Wabbajack", "GlobalHashCache.sqlite")));
            service.AddSingleton(new VFSCache(KnownFolders.EntryPoint.Combine("GlobalVFSCache3.sqlite")));
            service.AddSingleton<IRateLimiter>(new FixedSizeRateLimiter(2));
            service.AddDownloadDispatcher();
            service.AddSingleton<GameLocator>();
            return service;

        }
    }
}