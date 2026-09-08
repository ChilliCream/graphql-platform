namespace HotChocolate.Fusion.Policies.Rego;

public sealed class PollingRegoDataProviderTests
{
    [Fact]
    public async Task ChangeToken_Should_Fire_When_PollingIntervalElapses()
    {
        // arrange
        using var provider = new CountingPollingProvider(TimeSpan.FromMilliseconds(20));
        var token = provider.GetChangeToken();
        var fired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        token.RegisterChangeCallback(_ => fired.TrySetResult(), null);

        // act
        var completed = await Task.WhenAny(
            fired.Task,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        // assert
        Assert.Same(fired.Task, completed);
    }

    [Fact]
    public async Task GetChangeToken_Should_ReturnFreshToken_When_PreviousTokenHasFired()
    {
        // arrange
        using var provider = new CountingPollingProvider(TimeSpan.FromMilliseconds(20));
        var first = provider.GetChangeToken();

        // act
        var fired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        first.RegisterChangeCallback(_ => fired.TrySetResult(), null);
        await Task.WhenAny(
            fired.Task,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        var second = provider.GetChangeToken();

        // assert
        Assert.True(first.HasChanged);
        Assert.False(second.HasChanged);
    }

    [Fact]
    public void Dispose_Should_StopTimerAndCancelCurrentToken()
    {
        // arrange
        var provider = new CountingPollingProvider(TimeSpan.FromMilliseconds(20));
        var token = provider.GetChangeToken();

        // act
        provider.Dispose();

        // assert
        Assert.True(token.HasChanged);
    }

    [Fact]
    public void Ctor_Should_Throw_When_PollingIntervalIsNotPositive()
    {
        // act
        void Act() => new CountingPollingProvider(TimeSpan.Zero);

        // assert
        Assert.Throws<ArgumentOutOfRangeException>(Act);
    }

    private sealed class CountingPollingProvider(TimeSpan interval) : PollingRegoDataProvider(interval)
    {
        public override ValueTask<RegoDataSnapshot> GetDataAsync(CancellationToken cancellationToken)
            => ValueTask.FromResult(new RegoDataSnapshot("{}"u8, "v1"));
    }
}
