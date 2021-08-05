using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.Common
{
    public class StaticRateLimiter : IRateLimiter, IUnlockable
    {
        private readonly SemaphoreSlim _slim;

        public StaticRateLimiter(int max)
        {
            _slim = new SemaphoreSlim(max);
        }
        public async ValueTask<DisposableUnlockable> Checkout()
        {
            await _slim.WaitAsync();
            return new DisposableUnlockable(this);
        }
        public void Unlock()
        {
            _slim.Release();
        }
    }
}