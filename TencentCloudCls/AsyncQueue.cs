using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TencentCloudCls
{
    // similar to System.Threading.Channel, support .net framework
    internal class AsyncQueue<T>
    {
        private readonly SemaphoreSlim _notEmpty;
        private readonly SemaphoreSlim _notFull;
        private readonly Queue<T> _queue;

        public AsyncQueue(int capacity)
        {
            _notEmpty = new SemaphoreSlim(0);
            _notFull = new SemaphoreSlim(capacity);
            _queue = new Queue<T>(capacity);
        }

        public async Task<bool> EnqueueAsync(T o, TimeSpan timeout)
        {
            var ok = await _notFull.WaitAsync(timeout);
            if (!ok)
            {
                return false;
            }
            
            lock (this)
            {
                _queue.Enqueue(o);
            }

            _notEmpty.Release();
            return true;
        }

        public async Task<T> DequeueAsync()
        {
            await _notEmpty.WaitAsync();
            T o;

            lock (this)
            {
                o = _queue.Dequeue();
            }

            _notFull.Release();
            return o;
        }
    }
}