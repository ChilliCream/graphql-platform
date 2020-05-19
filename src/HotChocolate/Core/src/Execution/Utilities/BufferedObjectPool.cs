using System.Collections.Generic;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Utilities
{
    /// <summary>
    ///  A pool of objects. Bufferes a set of objects to ensure fast, thread safe object pooling
    /// </summary>
    /// <typeparam name="T"> The type of objects to pool.</typeparam>
    internal sealed class BufferedObjectPool<T> where T : class, new()
    {
        private readonly object _lockObject = new object();
        private readonly ObjectPool<ObjectBuffer<T>> _objectPool;
        private readonly List<ObjectBuffer<T>> _rentedBuffers =
            new List<ObjectBuffer<T>>();

        public BufferedObjectPool(
            ObjectPool<ObjectBuffer<T>> objectPool)
        {
            _objectPool = objectPool;
        }

        /// <summary>
        ///  Gets an object from the buffer if one is available, otherwise get a new buffer
        ///  from the pool one.
        /// </summary>
        /// <returns>A T.</returns>
        public T Get()
        {
            // We first try to pop the next resolver task safely from the pool queue
            if (!_rentedBuffers.TryPeek(out ObjectBuffer<T> buffer) ||
                !buffer.TryPopSafe(out T? obj))
            {
                // In case the buffer is empty we need to enque a new one.
                // We lock so it is ensure that in any case only one buffer is rented from the pool
                lock (_lockObject)
                {
                    // check if another thread already enqueed a not empty buffer before we locked
                    // in this case we do not need to perform thread safe operations as we are
                    // already in a lock
                    if (!_rentedBuffers.TryPeek(out buffer) ||
                        !buffer.TryPopSafe(out obj))
                    {
                        _rentedBuffers.Push(_objectPool.Get());
                        obj = _rentedBuffers.Peek().Pop();
                    }
                }
            }
            return obj;
        }

        /// <summary>
        ///  Return an object from the buffer if one is available. If the buffer is full
        ///  return the buffer to the pool
        /// </summary> 
        public void Return(T obj)
        {
            // if there is no buffer we let the object leak
            if (_rentedBuffers.TryPeek(out ObjectBuffer<T> buffer))
            {
                // We first try to push the resolver task safely to the buffer queue
                if (!buffer.TryPushSafe(obj))
                {
                    // In case the buffer is full we need to retun it to the pool
                    // We lock so it is ensure that in any case only one buffer is returnt to
                    // the pool
                    lock (_lockObject)
                    {
                        // as long as there are rented buffers take the first one an try to push
                        // the object on it. In case the buffer is full, return it to the pool
                        // and try with the next buffer.
                        while (_rentedBuffers.TryPeek(out buffer) && !buffer.TryPush(obj))
                        {
                            _objectPool.Return(_rentedBuffers.Pop());
                        }
                    }
                }
            }
        }

        public void Clear()
        {
            while (_rentedBuffers.TryPop(out ObjectBuffer<T>? buffer))
            {
                _objectPool.Return(buffer);
            }
        }
    }
}
