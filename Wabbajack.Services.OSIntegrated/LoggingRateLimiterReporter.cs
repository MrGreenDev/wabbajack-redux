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
        private long _reportNumber = 0;

        public LoggingRateLimiterReporter(ILogger<IRateLimiter> logger, IRateLimiter limiter)
        {
            _logger = logger;
            _limiter = limiter;
            _timer = new Timer(StartLoop, null, TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(1));
        }

        private void StartLoop(object? state)
        {
            _reportNumber += 1;
            
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

            var started = report.Reports.SelectMany(r => r.Value.JobReports.Values)
                .Count(r => r.Started);
            
            var pending = report.Reports.SelectMany(r => r.Value.JobReports.Values)
                .Count(r => !r.Started);

            
            var bleh = report.Reports.SelectMany(r => r.Value.JobReports.Values)
                .Where(r => r.Started)
                .Select(r => r.Description)
                .Distinct();
            foreach (var bl in bleh)
            {
                _logger.LogCritical(bl);
            }
            _logger.LogInformation("#{ReportNumber} [{Started}/{Pending}] Network: {Network}/sec, CPU: {Cpu}/sec, Disk: {Disk}/sec",
                _reportNumber, started, pending,
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