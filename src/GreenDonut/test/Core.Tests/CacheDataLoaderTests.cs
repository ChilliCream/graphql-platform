using Xunit;

namespace GreenDonut;

public class CacheDataLoaderTests
{
    [Fact]
    public async Task LoadSingleAsync()
    {
        // arrange
        using var cacheOwner = new PromiseCacheOwner();
        var dataLoader = new CustomCacheDataLoader(
            new DataLoaderOptions
            {
                Cache = cacheOwner.Cache
            });

        // act
        var result = await dataLoader.LoadAsync("abc");

        // assert
        Assert.Equal("Value:abc", result);
    }

    public class CustomCacheDataLoader(DataLoaderOptions options)
        : CacheDataLoader<string, string>(options)
    {
        protected override Task<string> LoadSingleAsync(
            string key,
            CancellationToken cancellationToken)
            => Task.FromResult("Value:" + key);
    }
}
