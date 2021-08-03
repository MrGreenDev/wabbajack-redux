using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;

namespace Wabbajack.Networking.Http.Test
{
    public class TestServerMessageSender : IHttpMessageSender
    {
        private readonly TestServer _testServer;
        private readonly HttpClient _client;

        public TestServerMessageSender(IServer ts)
        {
            _testServer = (TestServer)ts;
            _client = new HttpClient(new ConnectionLimitingDelegate(1, _testServer.CreateHandler()));
        }

        public Task<HttpResponseMessage> Send(HttpRequestMessage message)
        {
            return _client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
        }
    }
}