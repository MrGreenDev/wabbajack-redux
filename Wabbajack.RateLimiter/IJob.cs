using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.RateLimiter
{
    public interface IJob : IDisposable
    {
        public ulong ID { get; }
        public string Description { get; }
        public long Current { get; set; }
        public long Size { get; set; }
        bool Started { get; set; }
        public ValueTask<IMemoryOwner<byte>> Process(int size, CancellationToken token);
        public ValueTask Report(int processedSize, CancellationToken token);
    }
}