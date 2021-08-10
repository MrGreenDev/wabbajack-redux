using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.Common
{
    public interface IRateLimiter
    {
        /// <summary>
        ///     Returns true if the current task/thread of execution is being run from inside another worker queue
        /// </summary>
        public bool IsWorkerTask { get; }

        public Task<T> Enqueue<T>(Func<Task<T>> fn);
        public Task Enqueue(Func<Task> fn);

        public void Assist(CancellationToken token);
    }
}