using System;
using System.Collections.Generic;
using System.Threading;
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
                { Resource.Disk, new StandardRateLimitedResource(config.Disk, Resource.Disk) },
                { Resource.Network, new StandardRateLimitedResource(config.Network, Resource.Network)},
                { Resource.CPU , new StandardRateLimitedResource(config.CPU, Resource.CPU)}
            };

        }

        public async ValueTask<IJob> Begin(string jobTitle, long size, CancellationToken token, Resource resource)
        {
            var resourceLimiter = _resources[resource];
            return await resourceLimiter.Begin(jobTitle, size, token);
        }

        public async ValueTask<IJob> Begin(string jobTitle, long size, CancellationToken token,
            params Resource[] resources)
        {
            if (resources.Length == 1)
                return await Begin(jobTitle, size, token, resources[0]);
            
            var jobs = new IJob[resources.Length];
            Array.Sort(resources);
            for (var idx = 0; idx < resources.Length; idx++)
            {
                jobs[idx] = await Begin(jobTitle, size, token, resources[idx]);
            }

            return new CompositeJob(jobs);
        }

        public Report GetJobReports()
        {
            return new Report
            {
                Reports = new Dictionary<Resource, ResourceReport>()
                {
                    { Resource.CPU, _resources[Resource.CPU].GetReport() },
                    { Resource.Disk, _resources[Resource.Disk].GetReport() },
                    { Resource.Network, _resources[Resource.Network].GetReport() },
                }
            };
        }
    }
}