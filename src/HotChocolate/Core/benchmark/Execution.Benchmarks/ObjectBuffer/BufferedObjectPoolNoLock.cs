using System.Collections.Generic;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;
#nullable enable
namespace HotChocolate.Execution.Benchmarks
{
    internal sealed class BufferedObjectPoolNoLock<T> where T : class, new()
    {
        private readonly object _lockObject = new object();
        private readonly ObjectPool<ObjectBuffer<T>> _objectPool;
        private readonly List<ObjectBuffer<T>> _rentedBuffers =
            new List<ObjectBuffer<T>>();

        public BufferedObjectPoolNoLock(
            ObjectPool<ObjectBuffer<T>> objectPool)
        {
            _objectPool = objectPool;
        }

        public T Get()
        {
            if (!_rentedBuffers.TryPeek(out ObjectBuffer<T> buffer) ||
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
            if (_rentedBuffers.TryPeek(out ObjectBuffer<T> buffer))
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
            while (_rentedBuffers.TryPop(out ObjectBuffer<T>? buffer))
            {
                _objectPool.Return(buffer);
            }
        }
    }
}
