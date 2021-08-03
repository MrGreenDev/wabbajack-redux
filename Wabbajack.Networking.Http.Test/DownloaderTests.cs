using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Xunit;

namespace Wabbajack.Networking.Http.Test
{
    public class Tests
    {
        private readonly IServer _server;
        private readonly IHttpMessageSender _client;
        private readonly IHttpDownloader _downloader;

        public Tests(IServer server, IHttpMessageSender client, IHttpDownloader downloader)
        {
            _server = server;
            _client = client;
            _downloader = downloader;
        }
        
        [Fact]
        public async Task Test1()
        {
            using var response = await _client.Send(new HttpRequestMessage(HttpMethod.Get, "http://localhost/largeFile.bin"));
            
            Assert.True(response.IsSuccessStatusCode, response.ReasonPhrase);
            Assert.Equal(1024 * 1024 * 1024, response.Content.Headers.ContentLength);

            var tempFile = KnownFolders.EntryPoint.Combine("tempLargeFile");
            var hash = await _downloader.Download(response, tempFile, CancellationToken.None);
            Assert.Equal(response.Content.Headers.ContentLength, tempFile.Size());
            Assert.Equal(Hash.FromBase64("CcoJhlP48ic="), hash);
            
            tempFile.Delete();
        }
    }
}