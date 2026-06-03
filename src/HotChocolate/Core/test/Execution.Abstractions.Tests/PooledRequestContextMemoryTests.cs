using HotChocolate.Buffers;

namespace HotChocolate.Execution;

public class PooledRequestContextMemoryTests
{
    [Fact]
    public void AttachMemory_Should_SetMemory_When_Attached()
    {
        // arrange
        var context = new PooledRequestContext();
        using var arena = new MemoryArena();

        // act
        context.AttachMemory(arena);

        // assert
        Assert.Same(arena, context.Memory);
    }

    [Fact]
    public void AttachMemory_Should_Throw_When_AlreadyAttached()
    {
        // arrange
        var context = new PooledRequestContext();
        using var arena = new MemoryArena();
        using var other = new MemoryArena();
        context.AttachMemory(arena);

        // act
        void Act() => context.AttachMemory(other);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void DetachMemory_Should_ReturnArenaAndClearMemory_When_Attached()
    {
        // arrange
        var context = new PooledRequestContext();
        using var arena = new MemoryArena();
        context.AttachMemory(arena);

        // act
        var detached = context.DetachMemory();

        // assert
        Assert.Same(arena, detached);
        Assert.Null(context.Memory);
    }

    [Fact]
    public void DetachMemory_Should_Throw_When_NotAttached()
    {
        // arrange
        var context = new PooledRequestContext();

        // act
        void Act() => context.DetachMemory();

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Reset_Should_DisposeMemory_When_StillAttached()
    {
        // arrange
        // simulates the error or zero-event path, where the arena was never detached.
        var context = new PooledRequestContext();
        var arena = new MemoryArena();
        context.AttachMemory(arena);

        // act
        context.Reset();

        // assert
        Assert.True(arena.IsDisposed);
        Assert.Null(context.Memory);
    }

    [Fact]
    public void Reset_Should_NotDisposeMemory_When_AlreadyDetached()
    {
        // arrange
        // simulates the success path, where ownership was transferred to the result.
        var context = new PooledRequestContext();
        using var arena = new MemoryArena();
        context.AttachMemory(arena);
        context.DetachMemory();

        // act
        context.Reset();

        // assert
        Assert.False(arena.IsDisposed);
    }
}
