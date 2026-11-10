using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Mocha.Middlewares;
using Moq;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public sealed class RabbitMQPublishWaitTests
{
    [Fact]
    public async Task WaitForPublishChannelAsync_Should_ReturnOriginalChannel_When_DiagnosticIntervalElapses()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        await using var dispatcher = CreateDispatcher(timeProvider);
        var pending = new TaskCompletionSource<IChannel>(TaskCreationOptions.RunContinuationsAsynchronously);
        var channel = Mock.Of<IChannel>();

        // act
        var waiting = dispatcher.WaitForPublishChannelAsync(
            new ValueTask<IChannel>(pending.Task),
            new DispatchContext(),
            "ChannelCapacity").AsTask();
        timeProvider.Advance(TimeSpan.FromMinutes(1));
        var wasWaiting = !waiting.IsCompleted;
        pending.SetResult(channel);
        var result = await waiting.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // assert
        Assert.True(wasWaiting);
        Assert.Same(channel, result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WaitForPublishPhaseAsync_Should_PropagateOriginalFailure_When_OperationFails(bool timeout)
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        await using var dispatcher = CreateDispatcher(timeProvider);
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Exception expected = timeout ? new TimeoutException("broker timeout") : new IOException("connection lost");

        // act
        var waiting = dispatcher.WaitForPublishPhaseAsync(
            new ValueTask(pending.Task),
            new DispatchContext(),
            null,
            "PublisherConfirm").AsTask();
        timeProvider.Advance(TimeSpan.FromMinutes(1));
        pending.SetException(expected);
        var actual = await Record.ExceptionAsync(
            () => waiting.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

        // assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task WaitForPublishPhaseAsync_Should_PropagateCancellation_When_OperationIsCancelled()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        await using var dispatcher = CreateDispatcher(timeProvider);
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();

        // act
        var waiting = dispatcher.WaitForPublishPhaseAsync(
            new ValueTask(pending.Task),
            new DispatchContext(),
            null,
            "ReplyConsumer").AsTask();
        timeProvider.Advance(TimeSpan.FromMinutes(1));
        await cancellation.CancelAsync();
        pending.SetCanceled(cancellation.Token);

        // assert
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => waiting.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    private static RabbitMQDispatcher CreateDispatcher(TimeProvider timeProvider)
        => new(
            Mock.Of<ILogger<RabbitMQDispatcher>>(logger => logger.IsEnabled(LogLevel.Warning)),
            _ => ValueTask.FromResult(Mock.Of<IConnection>()),
            (_, _) => Task.CompletedTask)
        {
            TimeProvider = timeProvider
        };
}
