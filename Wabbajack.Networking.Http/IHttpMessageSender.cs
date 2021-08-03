using System.Net.Http;
using System.Threading.Tasks;

namespace Wabbajack.Networking.Http
{
    public interface IHttpMessageSender
    {
        public Task<HttpResponseMessage> Send(HttpRequestMessage message);
    }
}