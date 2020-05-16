using System.Collections.Generic;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;
#nullable enable
namespace HotChocolate.Execution.Benchmarks
{
    internal sealed class BufferedObjectPoolCStack<T> where T : class, new()
    {
        private readonly object _lockObject = new object();
        private readonly ObjectPool<ObjectBufferCStack<T>> _objectPool;
        private readonly List<ObjectBufferCStack<T>> _rentedBuffers =
            new List<ObjectBufferCStack<T>>();

        public BufferedObjectPoolCStack(
            ObjectPool<ObjectBufferCStack<T>> objectPool)
        {
            _objectPool = objectPool;
        }

        public T Get()
        {
            if (!_rentedBuffers.TryPeek(out ObjectBufferCStack<T> buffer) ||
                !buffer.TryPopSafe(out T? obj))
            {
                lock (_lockObject)
                {
                    if (!_rentedBuffers.TryPeek(out buffer) ||
                        !buffer.TryPopSafe(out obj))
                    {
                        _rentedBuffers.Push(_objectPool.Get());
                        obj = _rentedBuffers.Peek().PopSafe();
                    }
                }
            }
            return obj;
        }

        public void Return(T obj)
        {
            if (_rentedBuffers.TryPeek(out ObjectBufferCStack<T> buffer))
            {
                if (!buffer.TryPushSafe(obj))
                {
                    lock (_lockObject)
                    {
                        while (_rentedBuffers.TryPeek(out buffer) && !buffer.TryPushSafe(obj))
                        {
                            _objectPool.Return(_rentedBuffers.Pop());
                        }
                    }
                }
            }
        }

        public void Clear()
        {
            while (_rentedBuffers.TryPop(out ObjectBufferCStack<T>? buffer))
            {
                _objectPool.Return(buffer);
            }
        }
    }
}
