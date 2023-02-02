using Xunit;

namespace GreenDonut;

public class TaskCacheOwnerTests
{
    [Fact]
    public void EnsureTaskCacheIsReused()
    {
        // arrange
        var cacheOwner1 = new TaskCacheOwner();
        var cache = cacheOwner1.Cache;
        cacheOwner1.Dispose();

        // act
        using var cacheOwner2 = new TaskCacheOwner();

        // assert
        Assert.Same(cache, cacheOwner2.Cache);
    }

    [Fact]
    public void EnsureNewCachesAreIssued()
    {
        // arrange
        // act
        using var cacheOwner1 = new TaskCacheOwner();
        using var cacheOwner2 = new TaskCacheOwner();

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