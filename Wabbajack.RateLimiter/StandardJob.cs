using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.RateLimiter
{
    public class StandardJob : IJob
    {
        private readonly StandardRateLimitedResource _resource;
        public string Description { get; }
        public long Size { get; set; }
        
        private long _current;
        public long Current
        {
            get => _current;
            set => _current = value;
        }
        public ulong ID { get; }
        
        public bool Started { get; set; }


        public StandardJob(StandardRateLimitedResource resource, string jobTitle, long size, ulong id)
        {
            _resource = resource;
            Description = jobTitle;
            Size = size;
            _current = 0;
            ID = id;
        }

        public void Dispose()
        {
            _resource.Release(this);
        }


        public ValueTask<IMemoryOwner<byte>> Process(int requestedSize, CancellationToken token)
        {
            Interlocked.Add(ref _current, requestedSize);
            return _resource.Process(this, requestedSize, requestedSize, token);
        }

        public async ValueTask Report(int processedSize, CancellationToken token)
        {
            Interlocked.Add(ref _current, processedSize);
            await _resource.Process(this, processedSize, -1, token);
        }

    }
}