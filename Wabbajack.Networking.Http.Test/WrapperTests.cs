using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;
using Wabbajack.Networking.Http.Interfaces;
using Xunit;

namespace Wabbajack.Networking.Http.Test
{
    public class WrapperTests
    {
        private readonly IHttpMessageSender _client;
        private readonly ILogger<WrapperTests> _logger;

        public WrapperTests(IServer server, IHttpMessageSender client, IHttpDownloader downloader, ILogger<WrapperTests> logger)
        {
            _client = client;
            _logger = logger;
        }

        [Fact]
        public async Task TestMultipleConnections()
        {
            var tasks = Enumerable.Range(0, 10).Select(async i =>
            {
                using var result =
                    await _client.Send(new HttpRequestMessage(HttpMethod.Get, "http://localhost/countConnected"));
                _logger.LogInformation("Client returned {code}", result.StatusCode);
                return await result.Content.ReadAsStringAsync();
            }).ToList();

            var results = await Task.WhenAll(tasks);
            Assert.All(results, r => Assert.Equal("1", r));
        }
    }
}