using Xunit;

namespace HotChocolate.Utilities
{
    public class CacheTests
    {
        [Fact]
        public void Fill_Cache_Up()
        {
            // arrange
            var cache = new Cache<string>(10);
            for (var i = 0; i < 9; i++)
            {
                cache.GetOrCreate(i.ToString(), () => i.ToString());
            }

            // assert
            var value = cache.GetOrCreate("10", () => "10");

            // assert
            Assert.Equal("10", value);
            Assert.Equal(10, cache.Size);
            Assert.Equal(10, cache.Usage);
        }

        [Fact]
        public void Add_More_Items_To_The_Cache_Than_We_Have_Space()
        {
            // arrange
            var cache = new Cache<string>(10);
            for (var i = 0; i < 10; i++)
            {
                cache.GetOrCreate(i.ToString(), () => i.ToString());
            }

            // assert
            var value = cache.GetOrCreate("10", () => "10");

            // assert
            Assert.Equal("10", value);
            Assert.Equal(10, cache.Size);
            Assert.Equal(10, cache.Usage);
        }
    }
}
