namespace GreenDonut.Data;

public class ScriptedAsyncSourceTests
{
    [Fact]
    public async Task GetAsyncEnumerator_Should_YieldScriptedItemsInOrder_When_Enumerated()
    {
        // arrange
        var source = new ScriptedAsyncSource<string>("a", "b", "c");
        var items = new List<string>();

        // act
        await foreach (var item in source)
        {
            items.Add(item);
        }

        // assert
        Assert.Equal(["a", "b", "c"], items);
        Assert.Equal(["a", "b", "c"], source.Yielded);
        Assert.Equal(4, source.MoveNextCount);
    }

    [Fact]
    public async Task GateBeforeItem_Should_PauseEnumeration_Until_TestReleasesIt()
    {
        // arrange
        var source = new ScriptedAsyncSource<string>("a", "b");
        var gate = source.GateBeforeItem(1);
        await using var enumerator = source.GetAsyncEnumerator(TestContext.Current.CancellationToken);
        await enumerator.MoveNextAsync();

        // act: advancing to item 1 must not yield it until the gate is released
        var moveNextTask = enumerator.MoveNextAsync().AsTask();
        var yieldedWhilePaused = source.Yielded.ToArray();
        gate.SetResult();
        var moved = await moveNextTask;

        // assert
        Assert.Equal(["a"], yieldedWhilePaused);
        Assert.True(moved);
        Assert.Equal("b", enumerator.Current);
    }

    [Fact]
    public async Task ThrowAt_Should_ThrowScriptedException_Instead_Of_YieldingThatItem()
    {
        // arrange
        var source = new ScriptedAsyncSource<string>("a", "b", "c");
        var exception = new InvalidOperationException("boom");
        source.ThrowAt(1, exception);
        await using var enumerator = source.GetAsyncEnumerator(TestContext.Current.CancellationToken);
        await enumerator.MoveNextAsync();

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await enumerator.MoveNextAsync());

        // assert
        Assert.Same(exception, thrown);
        Assert.Equal(["a"], source.Yielded);
    }

    [Fact]
    public async Task DisposeAsync_Should_CountOneDisposal_PerEnumerator()
    {
        // arrange
        var source = new ScriptedAsyncSource<string>("a");
        var first = source.GetAsyncEnumerator(TestContext.Current.CancellationToken);
        var second = source.GetAsyncEnumerator(TestContext.Current.CancellationToken);

        // act
        await first.DisposeAsync();
        await second.DisposeAsync();

        // assert
        Assert.Equal(2, source.DisposeCount);
    }

    [Fact]
    public async Task ThrowOnDispose_Should_ThrowScriptedException_But_StillCountTheDisposal()
    {
        // arrange
        var source = new ScriptedAsyncSource<string>("a");
        var exception = new InvalidOperationException("dispose boom");
        source.ThrowOnDispose(exception);
        var enumerator = source.GetAsyncEnumerator(TestContext.Current.CancellationToken);

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await enumerator.DisposeAsync());

        // assert
        Assert.Same(exception, thrown);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task ScriptedAsyncDisposable_Should_CountEveryDisposeAsyncCall()
    {
        // arrange
        var lifetime = new ScriptedAsyncDisposable();

        // act
        await lifetime.DisposeAsync();
        await lifetime.DisposeAsync();

        // assert
        Assert.Equal(2, lifetime.DisposeCount);
    }

    [Fact]
    public async Task ScriptedAsyncDisposable_ThrowOnDispose_Should_ThrowScriptedException_But_StillCount()
    {
        // arrange
        var lifetime = new ScriptedAsyncDisposable();
        var exception = new InvalidOperationException("dispose boom");
        lifetime.ThrowOnDispose(exception);

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await lifetime.DisposeAsync());

        // assert
        Assert.Same(exception, thrown);
        Assert.Equal(1, lifetime.DisposeCount);
    }
}
