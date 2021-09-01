using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wabbajack.RateLimiter
{
    public class StandardRateLimiter : IRateLimiter
    {
        private readonly ResourceLimitConfiguration _config;
        private readonly Dictionary<Resource,StandardRateLimitedResource> _resources;

        public StandardRateLimiter(ResourceLimitConfiguration config)
        {
            _config = config;
            _resources = new Dictionary<Resource, StandardRateLimitedResource>()
            {
                { Resource.Disk, new StandardRateLimitedResource(config.Disk) },
                { Resource.Network, new StandardRateLimitedResource(config.Network)},
                { Resource.CPU , new StandardRateLimitedResource(config.CPU)}
            };

        }

        public async ValueTask<IJob> Begin(Resource resource, string jobTitle, long size)
        {
            var resourceLimiter = _resources[resource];
            return await resourceLimiter.Begin(jobTitle, size);
        }
    }
}