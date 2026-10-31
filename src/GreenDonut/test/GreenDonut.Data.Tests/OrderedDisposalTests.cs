using GreenDonut.Data.Internal;

namespace GreenDonut.Data;

public class OrderedDisposalTests
{
    [Fact]
    public async Task ReleaseAsync_Should_DisposeBoth_When_NeitherThrows()
    {
        // arrange
        var sourceDisposed = false;
        var lifetimeDisposed = false;

        // act
        await OrderedDisposal.ReleaseAsync(
            () =>
            {
                sourceDisposed = true;
                return ValueTask.CompletedTask;
            },
            () =>
            {
                lifetimeDisposed = true;
                return ValueTask.CompletedTask;
            });

        // assert
        Assert.True(sourceDisposed);
        Assert.True(lifetimeDisposed);
    }

    [Fact]
    public async Task ReleaseAsync_Should_Throw_TheSourceException_Unattached_When_OnlyTheSourceThrows()
    {
        // arrange
        var sourceException = new InvalidOperationException("source boom");
        var lifetimeDisposed = false;

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => OrderedDisposal.ReleaseAsync(
                () => throw sourceException,
                () =>
                {
                    lifetimeDisposed = true;
                    return ValueTask.CompletedTask;
                }).AsTask());

        // assert: the lifetime is still disposed even though the source threw
        Assert.Same(sourceException, thrown);
        Assert.True(lifetimeDisposed);
        Assert.Empty(OrderedDisposal.GetAttached(thrown));
    }

    [Fact]
    public async Task ReleaseAsync_Should_Throw_TheLifetimeException_Unattached_When_OnlyTheLifetimeThrows()
    {
        // arrange
        var lifetimeException = new InvalidOperationException("lifetime boom");

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => OrderedDisposal.ReleaseAsync(
                () => ValueTask.CompletedTask,
                () => throw lifetimeException).AsTask());

        // assert
        Assert.Same(lifetimeException, thrown);
        Assert.Empty(OrderedDisposal.GetAttached(thrown));
    }

    [Fact]
    public async Task ReleaseAsync_Should_Throw_TheSourceException_With_TheLifetimeExceptionAttached_When_BothThrow()
    {
        // arrange
        var sourceException = new InvalidOperationException("source boom");
        var lifetimeException = new InvalidOperationException("lifetime boom");

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => OrderedDisposal.ReleaseAsync(
                () => throw sourceException,
                () => throw lifetimeException).AsTask());

        // assert: the source's exception wins, with the lifetime's attached rather than replacing it
        Assert.Same(sourceException, thrown);
        Assert.Equal([lifetimeException], OrderedDisposal.GetAttached(thrown));
    }

    [Fact]
    public async Task ReleaseAsync_Should_Throw_TheSourceException_When_ThereIsNoLifetimeToDispose()
    {
        // arrange
        var sourceException = new InvalidOperationException("source boom");

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => OrderedDisposal.ReleaseAsync(() => throw sourceException, disposeLifetime: null).AsTask());

        // assert
        Assert.Same(sourceException, thrown);
    }

    [Fact]
    public void Attach_Should_AppendInOrder_When_CalledMoreThanOnce()
    {
        // arrange
        var fault = new InvalidOperationException("fault");
        var first = new InvalidOperationException("first");
        var second = new InvalidOperationException("second");

        // act
        OrderedDisposal.Attach(fault, first);
        OrderedDisposal.Attach(fault, second);

        // assert
        Assert.Equal([first, second], OrderedDisposal.GetAttached(fault));
    }

    [Fact]
    public void GetAttached_Should_ReturnEmpty_When_NothingWasAttached()
    {
        // arrange
        var fault = new InvalidOperationException("fault");

        // act
        var attached = OrderedDisposal.GetAttached(fault);

        // assert
        Assert.Empty(attached);
    }
}
