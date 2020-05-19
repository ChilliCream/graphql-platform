using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using Xunit;

namespace HotChocolate.Execution.Utilities
{
    public class BufferedObjectPoolTests
    {
        [Fact]
        public void PoolShouldCreateBuffer()
        {
            // arrange
            var pool = new TestPool(2, 4);
            var bufferedPool = new BufferedObjectPool<PoolElement>(pool);

            // act
            bufferedPool.Get();

            // assert  
            Assert.Single(pool.Rented);
            Assert.Empty(pool.Returned);
        }

        [Fact]
        public void PoolShouldCreateBufferWhenUsedUp()
        {
            // arrange
            var pool = new TestPool(2, 4);
            var bufferedPool = new BufferedObjectPool<PoolElement>(pool);

            // act
            bufferedPool.Get();
            bufferedPool.Get();
            bufferedPool.Get();
            bufferedPool.Get();

            // assert 
            Assert.Equal(2, pool.Rented.Count);
            Assert.Empty(pool.Returned);
        }

        [Fact]
        public void PoolShouldReturnBufferWhenNotLongerUsed()
        {
            // arrange
            var pool = new TestPool(2, 4);
            var bufferedPool = new BufferedObjectPool<PoolElement>(pool);

            // act
            PoolElement element1 = bufferedPool.Get();
            PoolElement element2 = bufferedPool.Get();
            bufferedPool.Get();
            Assert.Equal(2, pool.Rented.Count);
            Assert.Empty(pool.Returned);
            bufferedPool.Return(element1);
            bufferedPool.Return(element2);

            // assert  
            Assert.Single(pool.Rented);
            Assert.Single(pool.Returned);
        }

        private class PoolElement
        {

        }

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
