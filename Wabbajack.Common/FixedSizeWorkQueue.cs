using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.Common
{
    public class FixedSizeRateLimiter : IRateLimiter
    {
        private readonly int _size;
        private readonly AsyncBlockingCollection<Func<Task>> _queue = new();

        public FixedSizeRateLimiter(int size)
        {
            _size = size;
            StartWorkers(_size, CancellationToken.None);
        }

        private readonly AsyncLocal<bool> _isWorkerTask = new();

        public void Assist(CancellationToken token)
        {
            StartWorkers(1, token);
        }

        public bool IsWorkerTask => _isWorkerTask.Value;
        private void StartWorkers(int size, CancellationToken token)
        {
            for (var i = 0; i < size; i++)
            {
                Task.Run(async () =>
                {
                    _isWorkerTask.Value = true;
                    await foreach (var task in _queue.GetConsumingEnumerable().WithCancellation(token))
                    {
                        await task();
                    }
                }, token);
            }
        }

        public Task<T> Enqueue<T>(Func<Task<T>> fn)
        {
            TaskCompletionSource<T> tcs = new();
            _queue.Add(async () =>
            {
                try
                {
                    var result = await fn();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }
        
        public Task Enqueue(Func<Task> fn)
        {
            TaskCompletionSource tcs = new();
            _queue.Add(async () =>
            {
                try
                {
                    await fn();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }


    }
}