using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.Common
{
    public interface IRateLimiter
    {
        public ValueTask<DisposableUnlockable> Checkout();
    }
}