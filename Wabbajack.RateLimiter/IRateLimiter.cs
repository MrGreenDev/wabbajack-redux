using System;
using System.Threading.Tasks;

namespace Wabbajack.RateLimiter
{
    public interface IRateLimiter
    {
        public ValueTask<IJob> Begin(Resource resource, string jobTitle, long size);
    }
}