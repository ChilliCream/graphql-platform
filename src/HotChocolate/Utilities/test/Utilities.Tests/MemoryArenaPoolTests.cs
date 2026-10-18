namespace HotChocolate.Buffers;

[Collection(MemorySegmentTablePoolCollection.Name)]
public class MemoryArenaPoolTests
{
    [Fact]
    public void Rent_Should_ReturnRecycledOpenArena_When_PreviousArenaWasSealedAndDisposed()
    {
        // arrange
        var pool = new MemoryArenaPool(1);
        var first = pool.Rent();
        first.Rent(100);
        first.Seal();
        first.Dispose();

        // act
        var second = pool.Rent();
        var segment = second.Rent(200);

        // assert
        Assert.Same(first, second);
        Assert.False(second.IsSealed);
        Assert.Equal(0, segment.Offset);
        Assert.Equal(1, second.RentedPageCount);
    }

    [Fact]
    public void Rent_Should_CreateNewArena_When_PreviousArenaWasDisposedWithoutSeal()
    {
        // arrange
        var pool = new MemoryArenaPool(1);
        var first = pool.Rent();
        first.Rent(100);
        first.Dispose();

        // act
        var second = pool.Rent();

        // assert
        Assert.NotSame(first, second);
        Assert.True(first.IsDisposed);
        Assert.False(second.IsDisposed);
    }

    [Fact]
    public void Dispose_Should_DisposeArena_When_PoolIsFull()
    {
        // arrange
        var pool = new MemoryArenaPool(1);
        var first = pool.Rent();
        var second = pool.Rent();
        first.Seal();
        second.Seal();

        // act
        first.Dispose();
        second.Dispose();

        // assert
        Assert.False(first.IsDisposed);
        Assert.True(second.IsDisposed);
        Assert.Same(first, pool.Rent());
    }
}
