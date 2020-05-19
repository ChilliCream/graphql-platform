using System.Collections.Generic;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Utilities
{
    /// <summary>
    ///  A pool of objects. Buffers a set of objects to ensure fast, thread safe object pooling
    /// </summary>
    /// <typeparam name="T"> The type of objects to pool.</typeparam>
    internal sealed class BufferedObjectPool<T> : ObjectPool<T> where T : class, new()
    {
        private readonly object _lockObject = new object();
        private readonly List<ObjectBuffer<T>> _rentedBuffers = new List<ObjectBuffer<T>>();
        private readonly ObjectPool<ObjectBuffer<T>> _objectPool;

        public BufferedObjectPool(ObjectPool<ObjectBuffer<T>> objectPool)
        {
            _objectPool = objectPool;
        }

        /// <summary>
        ///  Gets an object from the buffer if one is available, otherwise get a new buffer
        ///  from the pool one.
        /// </summary>
        /// <returns>A T.</returns>
        public override T Get()
        {
            lock (_lockObject)
            {
                // We first try to pop the next resolver task safely from the pool queue
                if (!_rentedBuffers.TryPeek(out ObjectBuffer<T> buffer) ||
                    !buffer.TryPop(out T? obj))
                {
                    buffer = _objectPool.Get();
                    _rentedBuffers.Push(buffer);
                    obj = buffer.Pop();
                }
                return obj;
            }
        }

        /// <summary>
        ///  Return an object from the buffer if one is available. If the buffer is full
        ///  return the buffer to the pool
        /// </summary> 
        public override void Return(T obj)
        {
            lock (_lockObject)
            {
                // if there is no buffer we let the object leak
                if (_rentedBuffers.TryPeek(out ObjectBuffer<T> buffer) && !buffer.TryPush(obj))
                {
                    _objectPool.Return(_rentedBuffers.Pop());
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
