using System;
using System.Buffers;
using System.Threading.Tasks;

namespace Wabbajack.RateLimiter
{
    public interface IJob : IDisposable
    {
        public ValueTask<IMemoryOwner<byte>> Process(int size);
        public ValueTask Report(int processedSize);
    }
}