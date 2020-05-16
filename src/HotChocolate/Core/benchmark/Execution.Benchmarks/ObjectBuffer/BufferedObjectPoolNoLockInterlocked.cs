using System.Collections.Generic;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;
#nullable enable
namespace HotChocolate.Execution.Benchmarks
{
    internal sealed class BufferedObjectPoolNoLockInterlocked<T> where T : class, new()
    {
        private readonly object _lockObject = new object();
        private readonly ObjectPool<ObjectBufferNoLockInterlocked<T>> _objectPool;
        private readonly List<ObjectBufferNoLockInterlocked<T>> _rentedBuffers =
            new List<ObjectBufferNoLockInterlocked<T>>();

        public BufferedObjectPoolNoLockInterlocked(
            ObjectPool<ObjectBufferNoLockInterlocked<T>> objectPool)
        {
            _objectPool = objectPool;
        }

        public T Get()
        {
            if (!_rentedBuffers.TryPeek(out ObjectBufferNoLockInterlocked<T> buffer) ||
                !buffer.TryPop(out T? obj))
            {
                if (!_rentedBuffers.TryPeek(out buffer) ||
                    !buffer.TryPop(out obj))
                {
                    _rentedBuffers.Push(_objectPool.Get());
                    obj = _rentedBuffers.Peek().Pop();
                }
            }
            return obj;
        }

        public void Return(T obj)
        {
            if (_rentedBuffers.TryPeek(out ObjectBufferNoLockInterlocked<T> buffer))
            {
                if (!buffer.TryPush(obj))
                {
                    while (_rentedBuffers.TryPeek(out buffer) && !buffer.TryPush(obj))
                    {
                        _objectPool.Return(_rentedBuffers.Pop());
                    }
                }
            }
        }

        public void Clear()
        {
            while (_rentedBuffers.TryPop(out ObjectBufferNoLockInterlocked<T>? buffer))
            {
                _objectPool.Return(buffer);
            }
        }
    }
}
