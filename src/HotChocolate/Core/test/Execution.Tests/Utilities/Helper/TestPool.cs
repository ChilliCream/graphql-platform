using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Utilities
{
    internal class TestPool<T> : DefaultObjectPool<ObjectBuffer<T>>
        where T : class, new()
    {
        public List<ObjectBuffer<T>> Rented =
            new List<ObjectBuffer<T>>();

        public List<ObjectBuffer<T>> Returned =
            new List<ObjectBuffer<T>>();

        public TestPool(int bufferSize, int size)
            : base(new Policy(bufferSize), size)
        {
        }

        public override ObjectBuffer<T> Get()
        {
            ObjectBuffer<T> buffer = base.Get();
            Rented.Add(buffer);
            Returned.Remove(buffer);
            return buffer;
        }
        public override void Return(ObjectBuffer<T> obj)
        {
            Returned.Add(obj);
            Rented.Remove(obj);
            base.Return(obj);
        }

        private class Policy : IPooledObjectPolicy<ObjectBuffer<T>>
        {
            private int _bufferSize;

            public Policy(int bufferSize)
            {
                _bufferSize = bufferSize;
            }

            public ObjectBuffer<T> Create() =>
                new ObjectBuffer<T>(_bufferSize, x => { });

            public bool Return(ObjectBuffer<T> obj)
            {
                obj.Reset();
                return true;
            }
        }
    }
}
