using System;
using System.Buffers;
using System.Threading.Tasks;

namespace Wabbajack.RateLimiter
{
    public interface IJob : IDisposable
    {
        public ValueTask<IMemoryOwner<byte>> Process(Percent progress, int size);
        public ValueTask Report(Percent progress, int processedSize);
    }
}