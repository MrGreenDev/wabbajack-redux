using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Wabbajack.Common;
using Wabbajack.RateLimiter;

namespace Wabbajack.Services.OSIntegrated
{
    public class LoggingRateLimiterReporter : IDisposable
    {
        private readonly Timer _timer;
        private readonly ILogger<IRateLimiter> _logger;
        private readonly IRateLimiter _limiter;
        private Report _prevReport = new();

        public LoggingRateLimiterReporter(ILogger<IRateLimiter> logger, IRateLimiter limiter)
        {
            _logger = logger;
            _limiter = limiter;
            _timer = new Timer(StartLoop, null, TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(0.5));
        }

        private void StartLoop(object? state)
        {
            var report = _limiter.GetJobReports();

            var itms = new Dictionary<Resource, long>();

            var some = true;
            foreach (var (resource, resourceReport) in report.Reports)
            {
                long used = 0;
                if (_prevReport.Reports.TryGetValue(resource, out var prevResource))
                {

                    used = resourceReport.TotalUsed - prevResource.TotalUsed;
                }
                itms[resource] = used;
                if (used > 0)
                {
                    some = true;
                }
            }
            _logger.LogInformation("Network: {Network}/sec, CPU: {Cpu}/sec, Disk: {Disk}/sec",
                itms[Resource.Network].ToFileSizeString(), itms[Resource.CPU].ToFileSizeString(),
                itms[Resource.Disk].ToFileSizeString());
            if (some)
            {

            }
            _prevReport = report;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}