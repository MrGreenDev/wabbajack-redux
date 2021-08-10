using Microsoft.Extensions.DependencyInjection;

namespace Wabbajack.Installer
{
    public static class ServiceExtensions
    {
        public static void AddStandardInstaller(this IServiceCollection services)
        {
            services.AddScoped<IGameLocator, StubbedGameLocator>();
            services.AddScoped<InstallerConfiguration>();
            services.AddScoped<StandardInstaller>();
        }
    }
}