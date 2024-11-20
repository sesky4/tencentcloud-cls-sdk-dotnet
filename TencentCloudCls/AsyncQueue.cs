using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TencentCloudCls
{
    // similar to System.Threading.Channel, support .net framework
    public class AsyncQueue<T>
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

        public async Task EnqueueAsync(T o)
        {
            await _notFull.WaitAsync();
            lock (this)
            {
                _queue.Enqueue(o);
            }

            _notEmpty.Release();
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