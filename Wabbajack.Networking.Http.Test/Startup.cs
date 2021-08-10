using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;

namespace Wabbajack.Networking.Http.Test
{
    public class Startup
    {
        private static int clients;

        public void ConfigureHost(IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureWebHost(webHostBuilder => webHostBuilder
                .UseTestServer()
                .Configure(Configure)
                .ConfigureServices(services => services.AddRouting()));
        }

        public void ConfigureServices(IServiceCollection service)
        {
            service.AddSingleton<IHttpMessageSender, TestServerMessageSender>();
            service.AddSingleton<IHttpDownloader, SingleThreadedDownloader>();
            service.AddSingleton<TestServer, TestServer>();
        }

        private void Configure(IApplicationBuilder app)
        {
            var temp = CreateTempData();
            app.UseRouting()
                .UseStaticFiles(new StaticFileOptions
                {
                    ServeUnknownFileTypes = true,
                    RequestPath = "",
                    FileProvider = new PhysicalFileProvider(temp.ToString())
                })
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/internalError", async context => throw new Exception("Expected exception"));
                    endpoints.MapGet("/countConnected", async context =>
                    {
                        var total = Interlocked.Increment(ref clients);
                        await Task.Delay(250);
                        Interlocked.Decrement(ref clients);
                        await context.Response.WriteAsync(total.ToString());
                    });
                });
        }

        private static AbsolutePath CreateTempData()
        {
            var tempFolder = KnownFolders.EntryPoint.Combine("temp_serve");
            tempFolder.CreateDirectory();

            var data = new byte[1024 * 1024];

            for (var i = 0; i < data.Length; i++)
                data[i] = (byte)(i % 256);

            var stream = tempFolder.Combine("largeFile.bin").Open(FileMode.Create, FileAccess.Write);

            for (var i = 0; i < 1024; i++)
                stream.Write(data);

            return tempFolder;
        }
    }
}