using System;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class ObjectBufferPolicy<T>
        : IPooledObjectPolicy<ObjectBuffer<T>>
        where T : class, new()
    {
        private readonly int _capacity;
        private readonly Action<T> _clean;

        public ObjectBufferPolicy(int capacity, Action<T> clean)
        {
            _capacity = capacity;
            _clean = clean;
        }

        public ObjectBuffer<T> Create()
        {
            return new ObjectBuffer<T>(_capacity, _clean);
        }

        public bool Return(ObjectBuffer<T> obj)
        {
            obj.Reset();
            return true;
        }
    }
}
