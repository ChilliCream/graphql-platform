using Xunit;

namespace HotChocolate.Execution.Utilities
{
    public partial class BufferedObjectPoolTests
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
    }
}
