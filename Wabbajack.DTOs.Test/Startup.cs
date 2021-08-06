using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Wabbajack.DTOs.JsonConverters;

namespace Wabbajack.DTOs.Test
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDTOConverters();
            services.AddDTOSerializer();
            services.AddSingleton<HttpClient>();
        }
    }
}