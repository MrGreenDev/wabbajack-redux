using System;

namespace Wabbajack.RateLimiter
{
    public interface IRateLimiter
    {
        public IJob Begin(Resource resource, string jobTitle);
    }
}