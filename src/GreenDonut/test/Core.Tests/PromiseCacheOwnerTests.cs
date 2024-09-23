using Xunit;

namespace GreenDonut;

public class PromiseCacheOwnerTests
{
    [Fact]
    public void EnsureTaskCacheIsReused()
    {
        // arrange
        var pool = PromiseCachePool.Create();
        var cacheOwner1 = new PromiseCacheOwner(pool);
        var cache = cacheOwner1.Cache;
        cacheOwner1.Dispose();

        // act
        using var cacheOwner2 = new PromiseCacheOwner(pool);

        // assert
        Assert.Same(cache, cacheOwner2.Cache);
    }

    [Fact]
    public void EnsureNewCachesAreIssued()
    {
        // arrange
        var pool = PromiseCachePool.Create();

        // act
        using var cacheOwner1 = new PromiseCacheOwner(pool);
        using var cacheOwner2 = new PromiseCacheOwner(pool);

        // assert
        Assert.NotSame(cacheOwner1.Cache, cacheOwner2.Cache);
    }

    [Fact]
    public void DisposingTwoTimesWillNotThrow()
    {
        var cacheOwner = new PromiseCacheOwner();
        cacheOwner.Dispose();
        cacheOwner.Dispose();
    }
}
