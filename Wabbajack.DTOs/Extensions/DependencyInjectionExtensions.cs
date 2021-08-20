using Microsoft.Extensions.DependencyInjection;

namespace Wabbajack.DTOs
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddAllSingleton<T1, T2, TBase>(this IServiceCollection services)
            where TBase : class, T1, T2
            where T1 : class
            where T2 : class
        {
            services.AddSingleton<TBase>();
            services.AddSingleton<T1, TBase>(s => s.GetService<TBase>()!);
            services.AddSingleton<T2, TBase>(s => s.GetService<TBase>()!);
            return services;
        }
        
        public static IServiceCollection AddAllSingleton<T1, T2, T3, TBase>(this IServiceCollection services)
            where TBase : class, T1, T2, T3
            where T1 : class
            where T2 : class
            where T3 : class
        {
            services.AddSingleton<TBase>();
            services.AddSingleton<T1, TBase>(s => s.GetService<TBase>()!);
            services.AddSingleton<T2, TBase>(s => s.GetService<TBase>()!);
            services.AddSingleton<T3, TBase>(s => s.GetService<TBase>()!);
            return services;
        }
        
        public static IServiceCollection AddAllSingleton<T1, T2, T3, T4, TBase>(this IServiceCollection services)
            where TBase : class, T1, T2, T3, T4
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
        {
            services.AddSingleton<TBase>();
            services.AddSingleton<T1, TBase>(s => s.GetService<TBase>()!);
            services.AddSingleton<T2, TBase>(s => s.GetService<TBase>()!);
            services.AddSingleton<T3, TBase>(s => s.GetService<TBase>()!);
            services.AddSingleton<T4, TBase>(s => s.GetService<TBase>()!);
            return services;
        }
    }
}