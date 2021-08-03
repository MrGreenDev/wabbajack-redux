using System.Net.Http;

namespace Wabbajack.Networking.Http
{
    public interface IMessageGenerator
    {
        public HttpRequestMessage Generate();
    }
}