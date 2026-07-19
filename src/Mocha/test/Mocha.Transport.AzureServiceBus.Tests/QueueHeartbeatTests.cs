using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mocha.Transport.AzureServiceBus.Tests;

public class QueueHeartbeatTests
{
    [Fact]
    public async Task Loop_Should_InvokeKeepAliveOneAtATime_When_PeekIsSlow()
    {
        var invocationCount = 0;
        var concurrentInvocations = 0;
        var maxConcurrentInvocations = 0;
        var secondInvocation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task PeekAsync(CancellationToken cancellationToken)
        {
            var currentInvocations = Interlocked.Increment(ref concurrentInvocations);
            UpdateMaximum(ref maxConcurrentInvocations, currentInvocations);

            if (Interlocked.Increment(ref invocationCount) == 2)
            {
                secondInvocation.TrySetResult();
            }

            try
            {
                await Task.Delay(30);
            }
            finally
            {
                Interlocked.Decrement(ref concurrentInvocations);
            }
        }

        // arrange
        var heartbeat = new QueueHeartbeat(
            PeekAsync,
            TimeSpan.FromMilliseconds(10),
            NullLogger.Instance,
            "test-queue");

        // act
        await secondInvocation.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, Volatile.Read(ref maxConcurrentInvocations));
        Assert.True(Volatile.Read(ref invocationCount) >= 2);

        await heartbeat.DisposeAsync();
    }

    [Fact]
    public async Task Loop_Should_ContinueTicking_When_APeekThrows()
    {
        var invocationCount = 0;
        var successfulInvocationCount = 0;
        var successfulInvocation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task PeekAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref invocationCount) == 1)
            {
                throw new InvalidOperationException("Expected test failure.");
            }

            Interlocked.Increment(ref successfulInvocationCount);
            successfulInvocation.TrySetResult();
            return Task.CompletedTask;
        }

        // arrange
        var heartbeat = new QueueHeartbeat(
            PeekAsync,
            TimeSpan.FromMilliseconds(10),
            NullLogger.Instance,
            "test-queue");

        // act
        await successfulInvocation.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(Volatile.Read(ref invocationCount) > 1);
        Assert.True(Volatile.Read(ref successfulInvocationCount) > 0);

        await heartbeat.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_Should_AwaitInFlightPeek_When_PeekIsActive()
    {
        var completed = 0;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task PeekAsync(CancellationToken cancellationToken)
        {
            started.TrySetResult();
            await Task.Delay(250);
            Volatile.Write(ref completed, 1);
        }

        // arrange
        var heartbeat = new QueueHeartbeat(
            PeekAsync,
            TimeSpan.FromMilliseconds(10),
            NullLogger.Instance,
            "test-queue");

        // act
        await started.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken);
        await heartbeat.DisposeAsync();

        // assert
        Assert.Equal(1, Volatile.Read(ref completed));
    }

    [Fact]
    public async Task DisposeAsync_Should_StopFurtherTicks_When_Disposed()
    {
        var invocationCount = 0;
        var firstInvocation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task PeekAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref invocationCount);
            firstInvocation.TrySetResult();
            return Task.CompletedTask;
        }

        // arrange
        var heartbeat = new QueueHeartbeat(
            PeekAsync,
            TimeSpan.FromMilliseconds(10),
            NullLogger.Instance,
            "test-queue");

        // act
        await firstInvocation.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken);
        await heartbeat.DisposeAsync();
        var invocationCountAfterDisposal = Volatile.Read(ref invocationCount);
        await Task.Delay(75, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(invocationCountAfterDisposal, Volatile.Read(ref invocationCount));
    }

    private static void UpdateMaximum(ref int maximum, int value)
    {
        while (value > Volatile.Read(ref maximum))
        {
            var currentMaximum = Volatile.Read(ref maximum);

            if (value <= currentMaximum
                || Interlocked.CompareExchange(ref maximum, value, currentMaximum) == currentMaximum)
            {
                return;
            }
        }
    }
}
