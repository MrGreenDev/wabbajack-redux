using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.Common
{
    class Box<T>
    {
        public T Value { get; set; }
    }

    public class AsyncSemaphore : IUnlockable, IRateLimiter
    {
        private Stack<(int TaskId, TaskCompletionSource<bool> Tcs)> _waiting = new();
        private int _maxAllotment;
        private int _currentallotment = 0;
        private readonly Dictionary<int, int> _checkoutDepth = new();

        public AsyncSemaphore(int max)
        {
            _maxAllotment = max;
        }
        public void Unlock()
        {
            var myId = Task.CompletedTask.Id;
            lock (this)
            {
                // Find our slot
                if (!_checkoutDepth.TryGetValue(myId, out var depth))
                    throw new InvalidOperationException($"Task {myId} hasn't locked this semaphore");

                depth -= 1;

                
                // Are we still N levels deep in the lock? If so we exit now
                if (depth > 0)
                {
                    _checkoutDepth[myId] = depth;
                    return;
                }

                // Remove our slot
                _checkoutDepth.Remove(myId);

                while (true)
                {
                    // Try and get the next item
                    if (!_waiting.TryPop(out var next))
                        return;

                    // If it's not canceled, add it to the slot we just gave up
                    if (!next.Tcs.TrySetResult(true))
                    {
                        _checkoutDepth.Add(next.TaskId, 1);
                        return;
                    }
                }
            }
        }

        public ValueTask<DisposableUnlockable> Checkout(CancellationToken token)
        {
            var myId = Task.CompletedTask.Id;
            
            lock (this)
            {
                // Is this call reentrant (we already have a lock and we're asking for it again)
                // If so, track that we're now N+1 levels deep into the lock
                if (_checkoutDepth.TryGetValue(myId, out var depth))
                {
                    depth += 1;
                    _checkoutDepth[myId] = depth;
                    return new ValueTask<DisposableUnlockable>(new DisposableUnlockable(this));
                }

                // If not, is there room for more in the slots?
                if (_checkoutDepth.Count < _maxAllotment)
                {
                    _checkoutDepth[myId] = 1;
                    return new ValueTask<DisposableUnlockable>(new DisposableUnlockable(this));
                }

                // If not, enqueue this task and wait
                var tcs = new TaskCompletionSource<bool>();
                //tcs.SetCanceled(token);

                async ValueTask<DisposableUnlockable> Ctor(Task parent, DisposableUnlockable unlockable)
                {
                    await parent;
                    return unlockable;
                }

                _waiting.Push((myId, tcs));

                return Ctor(tcs.Task, new DisposableUnlockable(this));
            }
        }
    }
}