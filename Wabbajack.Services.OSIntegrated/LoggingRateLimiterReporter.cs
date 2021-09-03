using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Wabbajack.RateLimiter;

namespace Wabbajack.Services.OSIntegrated
{
    public class LoggingRateLimiterReporter : IDisposable
    {
        private readonly Timer _timer;
        private readonly ILogger<IRateLimiter> _logger;
        private readonly IRateLimiter _limiter;

        public LoggingRateLimiterReporter(ILogger<IRateLimiter> logger, IRateLimiter limiter)
        {
            _logger = logger;
            _limiter = limiter;
            _timer = new Timer(StartLoop, null, TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(1));
        }

        private void StartLoop(object? state)
        {
            var report = _limiter.GetJobReports();
            foreach (var (resource, resourceReport) in report.Reports.Where(line => line.Value.JobReports.Count > 0))
            {
                _logger.LogInformation("{Resource}: {Report}", resource, resourceReport.ToString());
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}