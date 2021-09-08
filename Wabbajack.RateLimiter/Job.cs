using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.RateLimiter
{
    public class Job<T> : IJob, IDisposable
    {
        public ulong ID { get; internal init; }
        public string Description { get; internal init; }
        public long Current { get; internal set; }
        public long? Size { get;  set; }
        public bool Started { get; internal set; }
        public IResource<T> Resource { get; init; }
        
        public async ValueTask Report(int processedSize, CancellationToken token)
        {
            await Resource.Report(this, processedSize, token);
        }

        public void Dispose()
        {
            Resource.Finish(this);
        }
    }
}