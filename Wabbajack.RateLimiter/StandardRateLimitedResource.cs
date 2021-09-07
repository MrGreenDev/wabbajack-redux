using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Wabbajack.RateLimiter
{
    public class StandardRateLimitedResource
    {
        private readonly ResourceLimit _limit;
        private readonly SemaphoreSlim _semaphore;
        private readonly Channel<PendingReport> _channel;
        private readonly ConcurrentDictionary<ulong, IJob> _tasks;
        private ulong _nextId = 0;
        private readonly Resource _resource;
        private long _totalUsed = 0;

        public StandardRateLimitedResource(ResourceLimit limit, Resource resource)
        {
            _resource = resource;
            _limit = limit;
            var calcLimit = _limit.MaxConcurrentTasks == -1 ? int.MaxValue : _limit.MaxConcurrentTasks;
            _semaphore = new SemaphoreSlim(calcLimit, calcLimit);
            _channel = Channel.CreateBounded<PendingReport>(10);
            _tasks = new ConcurrentDictionary<ulong, IJob>();
            var tsk = StartTask(CancellationToken.None);
        }

        private async ValueTask StartTask(CancellationToken token)
        {
            var sw = new Stopwatch();
            sw.Start();

            await foreach (var item in _channel.Reader.ReadAllAsync(token))
            {
                Interlocked.Add(ref _totalUsed, item.Size);
                if (_limit.MaxThroughput == -1)
                {
                    item.Result.TrySetResult(item.AllocSize == -1 ? null : MemoryPool<byte>.Shared.Rent(item.AllocSize));
                    sw.Restart();
                    continue;
                }
                
                var span = TimeSpan.FromSeconds((double)item.Size / _limit.MaxThroughput);
                

                await Task.Delay(span, token);
                
                sw.Restart();
                
                item.Result.TrySetResult(item.AllocSize == -1 ? null : MemoryPool<byte>.Shared.Rent(item.AllocSize));
            }
        }

        public async ValueTask<IJob> Begin(string jobTitle, long size, CancellationToken token)
        {
            var id = Interlocked.Increment(ref _nextId);
            var job = new StandardJob(this, jobTitle, size, id);
            _tasks.TryAdd(id, job);
            await _semaphore.WaitAsync(token);
            job.Started = true;
            return job;
        }

        public void Release(IJob job)
        {
            _semaphore.Release();
            _tasks.TryRemove(job.ID, out _);
        }

        public async ValueTask<IMemoryOwner<byte>> Process(StandardJob standardJob, int size, int allocSize, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<IMemoryOwner<byte>?>();
            await _channel.Writer.WriteAsync(new PendingReport
            {
                Job = standardJob,
                Size = size,
                AllocSize = allocSize,
                Result = tcs
            }, token);
            return (await tcs.Task)!;
        }

        struct PendingReport
        {
            public IJob Job { get; set; }
            public int Size { get; set; }
            
            public int AllocSize { get; set; }
            public TaskCompletionSource<IMemoryOwner<byte>?> Result { get; set; }
        }

        public ResourceReport GetReport()
        {
            return new ResourceReport
            {
                TotalUsed = _totalUsed,
                Resource = _resource,
                JobReports = _tasks.Select(j => new JobReport
                {
                    Id = j.Key,
                    Description = j.Value.Description,
                    Current = j.Value.Current,
                    Size = j.Value.Size
                }).ToDictionary(x => x.Id)
            };
        }
    }
}