using System;
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
        private readonly Action<T> _reset;
        private readonly T?[] _buffer = new T?[256];
        private readonly int _capacity;
        private int _index = -1;

        public BufferedObjectPool(Action<T> reset)
        {
            _reset = reset;
            _capacity = _buffer.Length - 1;
        }

        /// <summary>
        ///  Gets an object from the buffer if one is available, otherwise get a new buffer
        ///  from the pool one.
        /// </summary>
        /// <returns>A T.</returns>
        public override T Get()
        {
            T? item = default!;

            lock (_lockObject)
            {
                if (_index < _capacity)
                {
                    item = _buffer[++_index];
                }
            }

            if (item is null)
            {
                item = new T();
            }

            return item;
        }

        /// <summary>
        ///  Return an object from the buffer if one is available. If the buffer is full
        ///  return the buffer to the pool
        /// </summary> 
        public override void Return(T obj)
        {
            _reset(obj);

            lock (_lockObject)
            {
                if (_index > -1)
                {
                    _buffer[_index--] = obj;
                }
            }
        }
    }
}
