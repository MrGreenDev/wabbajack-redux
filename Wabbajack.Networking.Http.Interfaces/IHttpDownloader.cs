using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Paths;

namespace Wabbajack.Networking.Http.Interfaces
{
    public interface IHttpDownloader
    {
        public Task<Hash> Download(HttpResponseMessage message, AbsolutePath dest, CancellationToken token);
    }
}