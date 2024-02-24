using Xunit;

namespace GreenDonut;

public class TaskCacheOwnerTests
{
    [Fact]
    public void EnsureTaskCacheIsReused()
    {
        // arrange
        var pool = TaskCachePool.Create();
        var cacheOwner1 = new TaskCacheOwner(pool);
        var cache = cacheOwner1.Cache;
        cacheOwner1.Dispose();

        // act
        using var cacheOwner2 = new TaskCacheOwner(pool);

        // assert
        Assert.Same(cache, cacheOwner2.Cache);
    }

    [Fact]
    public void EnsureNewCachesAreIssued()
    {
        // arrange
        var pool = TaskCachePool.Create();
        
        // act
        using var cacheOwner1 = new TaskCacheOwner(pool);
        using var cacheOwner2 = new TaskCacheOwner(pool);

        // assert
        Assert.NotSame(cacheOwner1.Cache, cacheOwner2.Cache);
    }

    [Fact]
    public void DisposingTwoTimesWillNotThrow()
    {
        var cacheOwner = new TaskCacheOwner();
        cacheOwner.Dispose();
        cacheOwner.Dispose();
    }
}