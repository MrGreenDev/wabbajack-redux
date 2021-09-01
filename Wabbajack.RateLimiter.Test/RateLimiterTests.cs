using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wabbajack.RateLimiter;
using Xunit;
using static System.Threading.Tasks.Task;

namespace Wabbajack.RateLimiter.Test;

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
            new ParallelOptions {MaxDegreeOfParallelism = 10},
            async (x, token) =>
            { 
                using var job = await rateLimiter.Begin(Resource.CPU, "Incrementing", 1);
            SetMax(lockObj, ref current, ref max, 1);
            await Delay(10, token);
            SetMax(lockObj, ref current, ref max, -1);
        });
        
        Assert.Equal(2, max);

    }
}