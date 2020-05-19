using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Utilities
{
    public partial class BufferedObjectPoolTests
    {
        private class TestPool : DefaultObjectPool<ObjectBuffer<PoolElement>>
        {
            public List<ObjectBuffer<PoolElement>> Rented =
                new List<ObjectBuffer<PoolElement>>();

            public List<ObjectBuffer<PoolElement>> Returned =
                new List<ObjectBuffer<PoolElement>>();


            public TestPool(int bufferSize, int size)
                : base(new Policy(bufferSize), size)
            {
            }

            public override ObjectBuffer<PoolElement> Get()
            {
                ObjectBuffer<PoolElement> buffer = base.Get();
                Rented.Add(buffer);
                Returned.Remove(buffer);
                return buffer;
            }
            public override void Return(ObjectBuffer<PoolElement> obj)
            {
                Returned.Add(obj);
                Rented.Remove(obj);
                base.Return(obj);
            }

            private class Policy : IPooledObjectPolicy<ObjectBuffer<PoolElement>>
            {
                private int _bufferSize;

                public Policy(int bufferSize)
                {
                    _bufferSize = bufferSize;
                }

                public ObjectBuffer<PoolElement> Create() =>
                    new ObjectBuffer<PoolElement>(_bufferSize, x => { });

                public bool Return(ObjectBuffer<PoolElement> obj)
                {
                    obj.Reset();
                    return true;
                }
            }
        }
    }
}
