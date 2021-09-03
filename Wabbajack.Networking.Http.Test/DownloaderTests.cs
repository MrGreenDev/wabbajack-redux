using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Paths.IO;
using Xunit;

namespace Wabbajack.Networking.Http.Test
{
    public class Tests
    {
        private readonly IHttpMessageSender _client;
        private readonly IHttpDownloader _downloader;
        private readonly IServer _server;

        public Tests(IServer server, IHttpMessageSender client, IHttpDownloader downloader)
        {
            _server = server;
            _client = client;
            _downloader = downloader;
        }

        [Fact]
        public async Task Test1()
        {
            var msg = new HttpRequestMessage(HttpMethod.Get, "http://localhost/largeFile.bin");
            var tempFile = KnownFolders.EntryPoint.Combine("tempLargeFile");
            var hash = await _downloader.Download(msg, tempFile, CancellationToken.None);
            Assert.Equal(1024 * 1024 * 1024, tempFile.Size());
            Assert.Equal(Hash.FromBase64("KcPNNlWJ+7Y="), hash);

            tempFile.Delete();
        }
    }
}