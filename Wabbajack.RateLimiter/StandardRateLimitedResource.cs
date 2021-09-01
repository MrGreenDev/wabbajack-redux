using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Wabbajack.RateLimiter
{
    public class StandardRateLimitedResource
    {
        private readonly ResourceLimit _limit;
        private readonly SemaphoreSlim _semephore;
        private readonly Channel<PendingReport> _channel;
        private readonly ConcurrentBag<IJob> _tasks;

        public StandardRateLimitedResource(ResourceLimit limit)
        {
            _limit = limit;
            var calcLimit = _limit.MaxConcurrentTasks == -1 ? int.MaxValue : _limit.MaxConcurrentTasks;
            _semephore = new SemaphoreSlim(calcLimit, calcLimit);
            _channel = Channel.CreateBounded<PendingReport>(10);
            _tasks = new ConcurrentBag<IJob>();
            var tsk = StartTask(CancellationToken.None);
        }

        private async ValueTask StartTask(CancellationToken token)
        {
            var sw = new Stopwatch();

            await foreach (var item in _channel.Reader.ReadAllAsync(token))
            {
                var toWait = (int)(item.Size / (_limit.MaxThroughput * 1000) - sw.ElapsedMilliseconds);
                await Task.Delay(toWait, token);
                sw.Reset();
                
                item.Result.TrySetResult(item.Size == -1 ? null : MemoryPool<byte>.Shared.Rent(item.Size));
            }
        }

        public async ValueTask<IJob> Begin(string jobTitle, long size)
        {
            var job = new StandardJob(this, jobTitle, size);
            _tasks.Add(job);
            await _semephore.WaitAsync();
            return job;
        }

        public void Release()
        {
            _semephore.Release();
        }

        public async ValueTask<IMemoryOwner<byte>> Process(StandardJob standardJob, int size)
        {
            var tcs = new TaskCompletionSource<IMemoryOwner<byte>?>();
            await _channel.Writer.WriteAsync(new PendingReport
            {
                Job = standardJob,
                Size = size,
                Result = tcs
            });
            return (await tcs.Task)!;
        }

        struct PendingReport
        {
            public IJob Job { get; set; }
            public int Size { get; set; }
            public TaskCompletionSource<IMemoryOwner<byte>?> Result { get; set; }
        }
    }
}