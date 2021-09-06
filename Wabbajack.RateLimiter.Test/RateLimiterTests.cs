using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wabbajack.RateLimiter;
using Xunit;
using static System.Threading.Tasks.Task;

namespace Wabbajack.RateLimiter.Test
{

    public class RateLimiter
    {
        [Fact]
        public async Task BasicTaskTests()
        {
            var rateLimiter = new StandardRateLimiter(new ResourceLimitConfiguration()
                { CPU = { MaxConcurrentTasks = 2 } });

            var current = 0;
            var max = 0;
            object lockObj = new();

            void SetMax(object o, ref int i, ref int max1, int add)
            {
                lock (o)
                {
                    i += add;
                    max1 = Math.Max(i, max1);
                }
            }

            await Parallel.ForEachAsync(Enumerable.Range(0, 100),
                new ParallelOptions { MaxDegreeOfParallelism = 10 },
                async (x, token) =>
                {
                    using var job = await rateLimiter.Begin("Incrementing", 1, CancellationToken.None, Resource.CPU);
                    SetMax(lockObj, ref current, ref max, 1);
                    await Delay(10, token);
                    SetMax(lockObj, ref current, ref max, -1);
                });

            Assert.Equal(2, max);

        }

        [Fact]
        public async Task TestBasicThroughput()
        {
            var rateLimiter = new StandardRateLimiter(new ResourceLimitConfiguration()
                { CPU = { MaxConcurrentTasks = 1, MaxThroughput = 1024 * 1024 } });

            using var job = await rateLimiter.Begin( "Transferring", 1024 * 1024 * 5 / 2, CancellationToken.None, Resource.CPU);

            var sw = Stopwatch.StartNew();
            
            var report = rateLimiter.GetJobReports();
            Assert.Equal(0, report.Reports[Resource.CPU].TotalUsed);
            foreach (var x in Enumerable.Range(0, 5))
            {
                using var block = await job.Process(1024 * 1024 / 2, CancellationToken.None);
            }

            var elapsed = sw.Elapsed;
            Assert.True(elapsed > TimeSpan.FromSeconds(2));
            Assert.True(elapsed < TimeSpan.FromSeconds(3));

            report = rateLimiter.GetJobReports();
            Assert.Equal(1024 * 1024 * 5 / 2, report.Reports[Resource.CPU].TotalUsed);
        }

        [Fact]
        public async Task TestParallelThroughput()
        {
            var rateLimiter = new StandardRateLimiter(new ResourceLimitConfiguration()
                { CPU = { MaxConcurrentTasks = 2, MaxThroughput = 1024 * 1024 } });



            var sw = Stopwatch.StartNew();

            var tasks = new List<Task>();
            for (var i = 0; i < 4; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var job = await rateLimiter.Begin("Transferring", 1024 * 1024 / 10 * 5, CancellationToken.None, Resource.CPU);
                    for (var x = 0; x < 5; x++)
                    {
                        using var block = await job.Process(1024 * 1024 / 10, CancellationToken.None);
                    }
                }));
            }

            await WhenAll(tasks.ToArray());
            var elapsed = sw.Elapsed;
            Assert.True(elapsed > TimeSpan.FromSeconds(2));
            Assert.True(elapsed < TimeSpan.FromSeconds(3));
        }

        [Fact]
        public async Task TestParallelThroughputWithLimitedTasks()
        {
            var rateLimiter = new StandardRateLimiter(new ResourceLimitConfiguration()
                { CPU = { MaxConcurrentTasks = 2, MaxThroughput = 1024 * 1024 * 4 } });

            var sw = Stopwatch.StartNew();

            var tasks = new List<Task>();
            for (var i = 0; i < 4; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var job = await rateLimiter.Begin("Transferring", 1024 * 1024 / 10 * 5,CancellationToken.None, Resource.CPU);
                    for (var x = 0; x < 5; x++)
                    {
                        using var block = await job.Process(1024 * 1024 / 10, CancellationToken.None);
                    }
                }));
            }

            await WhenAll(tasks.ToArray());
            var elapsed = sw.Elapsed;
            Assert.True(elapsed > TimeSpan.FromSeconds(0.5));
            Assert.True(elapsed < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task CanGetProgressReport()
        {
            var rateLimiter = new StandardRateLimiter(new ResourceLimitConfiguration()
                { CPU = { MaxConcurrentTasks = 2, MaxThroughput = 1024 * 1024 * 4 } });

            List<Report> reports = new();
            var timer = new Timer(x => { reports.Add(rateLimiter.GetJobReports()); }, null, TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(0.25));


            var sw = Stopwatch.StartNew();

            var tasks = new List<Task>();
            for (var i = 0; i < 4; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var job = await rateLimiter.Begin("Transferring", 1024 * 1024 / 10 * 5, CancellationToken.None
                        , Resource.CPU, Resource.Disk, Resource.Network);
                    for (var x = 0; x < 5; x++)
                    {
                        using var block = await job.Process(1024 * 1024 / 10, CancellationToken.None);
                    }
                }));
            }

            await WhenAll(tasks.ToArray());
            await timer.DisposeAsync();

            var elapsed = sw.Elapsed;
            Assert.True(elapsed > TimeSpan.FromSeconds(0.5));
            Assert.True(elapsed < TimeSpan.FromSeconds(1));

        }
    }
}