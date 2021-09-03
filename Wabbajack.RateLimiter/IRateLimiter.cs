using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.RateLimiter
{
    public interface IRateLimiter
    {
        public ValueTask<IJob> Begin(string jobTitle, long size, CancellationToken token, params Resource[] resources);
        public Report GetJobReports();
    }
}