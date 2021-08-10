using System;
using System.Collections.Generic;
using System.Threading;

namespace Wabbajack.Common
{
    public class AsyncBlockingCollection<T>
    {
        private readonly Stack<T> _queue = new();
        private readonly SemaphoreSlim _semaphore = new(0);
        private int _consumersCount;
        private bool _isAddingCompleted;

        public void Add(T item)
        {
            lock (_queue)
            {
                if (_isAddingCompleted) throw new InvalidOperationException();
                _queue.Push(item);
            }

            _semaphore.Release();
        }

        public void CompleteAdding()
        {
            lock (_queue)
            {
                if (_isAddingCompleted) return;
                _isAddingCompleted = true;
                if (_consumersCount > 0) _semaphore.Release(_consumersCount);
            }
        }

        public async IAsyncEnumerable<T> GetConsumingEnumerable()
        {
            lock (_queue)
            {
                _consumersCount++;
            }

            while (true)
            {
                lock (_queue)
                {
                    if (_queue.Count == 0 && _isAddingCompleted) break;
                }

                await _semaphore.WaitAsync();
                bool hasItem;
                T item = default;
                lock (_queue)
                {
                    hasItem = _queue.Count > 0;
                    if (hasItem) item = _queue.Pop();
                }

                if (hasItem) yield return item;
            }
        }
    }
}