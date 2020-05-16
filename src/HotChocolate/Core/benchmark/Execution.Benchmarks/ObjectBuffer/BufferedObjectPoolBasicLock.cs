using System.Collections.Generic;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

#nullable enable
namespace HotChocolate.Execution.Benchmarks
{
    internal sealed class BufferedObjectPoolBasicLock<T> where T : class, new()
    {
        private readonly ObjectPool<ObjectBufferBasicLock<T>> _objectPool;
        private readonly List<ObjectBufferBasicLock<T>> _rentedBuffers =
            new List<ObjectBufferBasicLock<T>>();

        public BufferedObjectPoolBasicLock(
            ObjectPool<ObjectBufferBasicLock<T>> objectPool)
        {
            _objectPool = objectPool;
        }

        public T Get()
        {
            if (!_rentedBuffers.TryPeek(out ObjectBufferBasicLock<T> buffer) ||
                !buffer.TryPopSafe(out T? obj))
            {
                if (!_rentedBuffers.TryPeek(out buffer) ||
                    !buffer.TryPopSafe(out obj))
                {
                    _rentedBuffers.Push(_objectPool.Get());
                    obj = _rentedBuffers.Peek().Pop();
                }
            }
            return obj;
        }

        public void Return(T obj)
        {
            if (_rentedBuffers.TryPeek(out ObjectBufferBasicLock<T> buffer))
            {
                if (!buffer.TryPushSafe(obj))
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
            while (_rentedBuffers.TryPop(out ObjectBufferBasicLock<T>? buffer))
            {
                _objectPool.Return(buffer);
            }
        }
    }
}
