namespace HotChocolate.Fusion.Execution.Pipeline;

public class RequestTimeoutSourceTests
{
    [Fact]
    public async Task Token_Should_BeCanceledAsTimeout_When_ReusedSourceRunsIntoTimeout()
    {
        // arrange
        using var source = new RequestTimeoutSource(CancellationToken.None);
        source.Start(CancellationToken.None, TimeSpan.FromSeconds(30));
        source.StopTimeout();
        var reset = source.TryReset();

        // act
        source.Start(CancellationToken.None, TimeSpan.FromMilliseconds(50));
        var canceled = await WaitForCancellationAsync(source.Token);

        // assert
        Assert.True(reset);
        Assert.True(canceled);
        Assert.True(source.IsTimedOut);
        Assert.False(source.TryReset());
    }

    [Fact]
    public async Task Token_Should_NotBeCanceled_When_TimeoutOfPreviousRequestElapses()
    {
        // arrange
        using var source = new RequestTimeoutSource(CancellationToken.None);
        source.Start(CancellationToken.None, TimeSpan.FromMilliseconds(50));
        source.StopTimeout();
        var reset = source.TryReset();

        // act
        source.Start(CancellationToken.None, TimeSpan.FromSeconds(30));
        await Task.Delay(250, TestContext.Current.CancellationToken);

        // assert
        Assert.True(reset);
        Assert.False(source.Token.IsCancellationRequested);
        Assert.False(source.IsTimedOut);
        Assert.True(source.TryReset());
    }

    [Fact]
    public void Token_Should_BeCanceled_When_RequestIsAborted()
    {
        // arrange
        using var source = new RequestTimeoutSource(CancellationToken.None);
        using var requestAborted = new CancellationTokenSource();
        source.Start(requestAborted.Token, TimeSpan.FromSeconds(30));

        // act
        requestAborted.Cancel();

        // assert
        Assert.True(source.Token.IsCancellationRequested);
        Assert.False(source.IsTimedOut);
        Assert.False(source.TryReset());
    }

    [Fact]
    public void Token_Should_BeCanceled_When_SchemaShutsDown()
    {
        // arrange
        using var shutdown = new CancellationTokenSource();
        using var source = new RequestTimeoutSource(shutdown.Token);
        source.Start(CancellationToken.None, TimeSpan.FromSeconds(30));

        // act
        shutdown.Cancel();

        // assert
        Assert.True(source.Token.IsCancellationRequested);
        Assert.False(source.IsTimedOut);
        Assert.False(source.TryReset());
    }

    [Fact]
    public void Token_Should_NotObserveAbort_When_SourceWasReset()
    {
        // arrange
        using var source = new RequestTimeoutSource(CancellationToken.None);
        using var previousRequestAborted = new CancellationTokenSource();
        source.Start(previousRequestAborted.Token, TimeSpan.FromSeconds(30));
        source.StopTimeout();
        var reset = source.TryReset();
        source.Start(CancellationToken.None, TimeSpan.FromSeconds(30));

        // act
        previousRequestAborted.Cancel();

        // assert
        Assert.True(reset);
        Assert.False(source.Token.IsCancellationRequested);
    }

    private static async Task<bool> WaitForCancellationAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), token);
            return false;
        }
        catch (OperationCanceledException)
        {
            return true;
        }
    }
}
