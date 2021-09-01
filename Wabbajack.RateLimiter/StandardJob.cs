using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.RateLimiter
{
    public class StandardJob : IJob
    {
        private readonly StandardRateLimitedResource _resource;
        private readonly string _title;
        private readonly long _size;
        private long _processed;

        public StandardJob(StandardRateLimitedResource resource, string jobTitle, long size)
        {
            _resource = resource;
            _title = jobTitle;
            _size = size;
            _processed = 0;
        }

        public void Dispose()
        {
            _resource.Release();
        }

        public ValueTask<IMemoryOwner<byte>> Process(int requestedSize)
        {
            Interlocked.Add(ref _processed, requestedSize);
            return _resource.Process(this, requestedSize);
        }

        public async ValueTask Report(int processedSize)
        {
            Interlocked.Add(ref _processed, processedSize);
            await _resource.Process(this, processedSize);
        }

    }
}