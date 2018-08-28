using Xunit;

namespace HotChocolate.Runtime
{
    public class CacheTests
    {
        [Fact]
        public void CacheItemRemoved()
        {
            // arrange
            string removedValue = null;
            Cache<string> cache = new Cache<string>(10);
            cache.RemovedEntry += (s, e) => { removedValue = e.Value; };
            for (int i = 0; i < 10; i++)
            {
                cache.GetOrCreate(i.ToString(), () => i.ToString());
            }

            // assert
            string value = cache.GetOrCreate("10", () => "10");

            // assert
            Assert.Equal("10", value);
            Assert.Equal("0", removedValue);
            Assert.Equal(10, cache.Size);
            Assert.Equal(10, cache.Usage);
        }
    }
}
