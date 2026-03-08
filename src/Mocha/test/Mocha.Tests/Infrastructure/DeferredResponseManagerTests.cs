using Microsoft.Extensions.Time.Testing;
using Mocha.Events;

namespace Mocha.Tests;

public class DeferredResponseManagerTests
{
    [Fact]
    public async Task AddPromise_Should_CompletePromiseAndReturnResult_When_PromiseCompleted()
    {
        // arrange
        var manager = new DeferredResponseManager(TimeProvider.System);
        var correlationId = Guid.NewGuid().ToString();

        // act
        var tcs = manager.AddPromise(correlationId, TimeSpan.FromSeconds(30));
        manager.CompletePromise(correlationId, "result-value");

        // assert
        var result = await tcs.Task;
        Assert.Equal("result-value", result);
    }

    [Fact]
    public async Task CompletePromise_Should_ReturnTrue_When_PromiseCompleted()
    {
        // arrange
        var manager = new DeferredResponseManager(TimeProvider.System);
        var correlationId = Guid.NewGuid().ToString();
        manager.AddPromise(correlationId, TimeSpan.FromSeconds(30));

        // act
        var completed = manager.CompletePromise(correlationId, "result");

        // assert
        Assert.True(completed);
    }

    [Fact]
    public void CompletePromise_Should_ReturnFalse_When_PromiseDoesNotExist()
    {
        // arrange
        var manager = new DeferredResponseManager(TimeProvider.System);
        var nonExistentId = Guid.NewGuid().ToString();

        // act
        var completed = manager.CompletePromise(nonExistentId, "result");

        // assert
        Assert.False(completed);
    }

    [Fact]
    public async Task AddPromise_Should_CreatePendingTask_When_PromiseAdded()
    {
        // arrange
        var manager = new DeferredResponseManager(TimeProvider.System);
        var correlationId = Guid.NewGuid().ToString();

        // act
        var tcs = manager.AddPromise(correlationId, TimeSpan.FromSeconds(30));

        // assert
        Assert.False(tcs.Task.IsCompleted);
        Assert.False(tcs.Task.IsFaulted);
        Assert.False(tcs.Task.IsCanceled);

        // cleanup - complete it so test doesn't hang
        manager.CompletePromise(correlationId, null);
        await tcs.Task;
    }

    [Fact]
    public async Task AddPromise_Should_UseDefaultTimeout_When_TimeoutIsNull()
    {
        // arrange
        var manager = new DeferredResponseManager(TimeProvider.System);
        var correlationId = Guid.NewGuid().ToString();

        // act
        var tcs = manager.AddPromise(correlationId, null);

        // assert - task should be pending (default timeout is 2 minutes)
        Assert.False(tcs.Task.IsCompleted);

        // cleanup
        manager.CompletePromise(correlationId, null);
        await tcs.Task;
    }

    [Fact]
    public async Task SetException_Should_RejectPromise_When_ExceptionSet()
    {
        // arrange
        var manager = new DeferredResponseManager(TimeProvider.System);
        var correlationId = Guid.NewGuid().ToString();
        var tcs = manager.AddPromise(correlationId, TimeSpan.FromSeconds(30));

        // act
        manager.SetException(correlationId, new InvalidOperationException("test error"));

        // assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tcs.Task);
        Assert.Equal("test error", ex.Message);
    }

    [Fact]
    public void SetException_Should_BeNoOp_When_PromiseDoesNotExist()
    {
        // arrange
        var manager = new DeferredResponseManager(TimeProvider.System);
        var nonExistentId = Guid.NewGuid().ToString();

        // act & assert - should not throw
        manager.SetException(nonExistentId, new InvalidOperationException("test"));
    }

    [Fact]
    public async Task GetPromise_Should_ReturnCompletedResult_When_PromiseCompleted()
    {
        // arrange
        var manager = new DeferredResponseManager(TimeProvider.System);
        var correlationId = Guid.NewGuid().ToString();
        manager.AddPromise(correlationId, TimeSpan.FromSeconds(30));

        // act - complete it from another task so GetPromise can retrieve it
        var getTask = manager.GetPromise(correlationId);
        manager.CompletePromise(correlationId, "response-data");
        var result = await getTask;

        // assert
        Assert.Equal("response-data", result);
    }

    [Fact]
    public async Task GetPromise_Should_WaitForPromiseToComplete_When_PromiseAdded()
    {
        // arrange
        var manager = new DeferredResponseManager(TimeProvider.System);
        var correlationId = Guid.NewGuid().ToString();
        manager.AddPromise(correlationId, TimeSpan.FromSeconds(30));

        // act - GetPromise synchronously looks up the TCS then returns a task awaiting it
        var getTask = manager.GetPromise(correlationId);
        manager.CompletePromise(correlationId, "delayed-result");

        // assert
        var result = await getTask;
        Assert.Equal("delayed-result", result);
    }

    [Fact]
    public async Task GetPromise_Should_Throw_When_PromiseDoesNotExist()
    {
        // arrange
        var manager = new DeferredResponseManager(TimeProvider.System);
        var nonExistentId = Guid.NewGuid().ToString();

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.GetPromise(nonExistentId));
    }

    [Fact]
    public async Task Promise_Should_Timeout_When_TimeAdvancesPastTimeout()
    {
        // arrange
        var fakeTime = new FakeTimeProvider();
        var manager = new DeferredResponseManager(fakeTime);
        var correlationId = Guid.NewGuid().ToString();

        var tcs = manager.AddPromise(correlationId, TimeSpan.FromSeconds(5));

        // act - advance time past timeout
        fakeTime.Advance(TimeSpan.FromSeconds(10));

        // assert - task should be faulted with ResponseTimeoutException
        var ex = await Assert.ThrowsAsync<ResponseTimeoutException>(() => tcs.Task);
        Assert.Equal(correlationId, ex.CorrelationId);
    }

    [Fact]
    public async Task Promise_Should_RemainsActive_When_TimeAdvancesWithinTimeout()
    {
        // arrange
        var fakeTime = new FakeTimeProvider();
        var manager = new DeferredResponseManager(fakeTime);
        var expiredId = Guid.NewGuid().ToString();
        var validId = Guid.NewGuid().ToString();

        var expiredTcs = manager.AddPromise(expiredId, TimeSpan.FromSeconds(5));
        var validTcs = manager.AddPromise(validId, TimeSpan.FromSeconds(20));

        // act - advance time to expire first but not second
        fakeTime.Advance(TimeSpan.FromSeconds(10));

        // assert
        await Assert.ThrowsAsync<ResponseTimeoutException>(() => expiredTcs.Task);
        Assert.False(validTcs.Task.IsCompleted, "Valid promise should still be pending");

        // cleanup
        manager.CompletePromise(validId, null);
        await validTcs.Task;
    }

    [Fact]
    public async Task Promise_Should_TimeoutWithShortTimeout_When_TimeAdvancesPastTimeout()
    {
        // arrange
        var fakeTime = new FakeTimeProvider();
        var manager = new DeferredResponseManager(fakeTime);
        var correlationId = Guid.NewGuid().ToString();

        // act
        var tcs = manager.AddPromise(correlationId, TimeSpan.FromMilliseconds(100));
        fakeTime.Advance(TimeSpan.FromMilliseconds(150));

        // assert
        await Assert.ThrowsAsync<ResponseTimeoutException>(() => tcs.Task);
    }

    [Fact]
    public async Task Promise_Should_NotThrow_When_TimedOutTwice()
    {
        // arrange
        var fakeTime = new FakeTimeProvider();
        var manager = new DeferredResponseManager(fakeTime);
        var correlationId = Guid.NewGuid().ToString();
        var tcs = manager.AddPromise(correlationId, TimeSpan.FromSeconds(5));

        // act - advance past timeout, then advance again
        fakeTime.Advance(TimeSpan.FromSeconds(10));
        fakeTime.Advance(TimeSpan.FromSeconds(10));

        // assert
        await Assert.ThrowsAsync<ResponseTimeoutException>(() => tcs.Task);
    }

    [Fact]
    public async Task CompletePromise_Should_NotFault_When_CompletedBeforeTimeout()
    {
        // arrange
        var fakeTime = new FakeTimeProvider();
        var manager = new DeferredResponseManager(fakeTime);
        var correlationId = Guid.NewGuid().ToString();

        var tcs = manager.AddPromise(correlationId, TimeSpan.FromSeconds(5));

        // act - complete before timeout, then advance past the original timeout
        manager.CompletePromise(correlationId, "completed-value");
        fakeTime.Advance(TimeSpan.FromSeconds(10));

        // assert - should have the completed value, not a timeout exception
        var result = await tcs.Task;
        Assert.Equal("completed-value", result);
    }

    [Fact]
    public async Task Concurrent_AddPromise_And_CompletePromise_Should_CompleteConcurrently()
    {
        // arrange
        var manager = new DeferredResponseManager(TimeProvider.System);
        var tasks = new List<Task>();

        // act - add and complete 50 promises concurrently
        for (var i = 0; i < 50; i++)
        {
            var correlationId = $"concurrent-{i}";
            tasks.Add(
                Task.Run(async () =>
                {
                    var tcs = manager.AddPromise(correlationId, TimeSpan.FromSeconds(30));
                    await Task.Delay(Random.Shared.Next(1, 10), default);
                    manager.CompletePromise(correlationId, $"result-{correlationId}");
                    var result = await tcs.Task;
                    Assert.Equal($"result-{correlationId}", result);
                }, default));
        }

        // assert - all should complete without error
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task CompletePromise_Should_OnlySucceed_When_CalledFirst()
    {
        // arrange
        var manager = new DeferredResponseManager(TimeProvider.System);
        var correlationId = Guid.NewGuid().ToString();
        var tcs = manager.AddPromise(correlationId, TimeSpan.FromSeconds(30));

        // act
        var firstComplete = manager.CompletePromise(correlationId, "first");
        var secondComplete = manager.CompletePromise(correlationId, "second");

        // assert
        Assert.True(firstComplete);
        Assert.False(secondComplete);
        var result = await tcs.Task;
        Assert.Equal("first", result);
    }

    [Fact]
    public async Task CompletePromise_Should_ReturnFalse_When_SetExceptionIsCalledAfter()
    {
        // arrange
        var manager = new DeferredResponseManager(TimeProvider.System);
        var correlationId = Guid.NewGuid().ToString();
        var tcs = manager.AddPromise(correlationId, TimeSpan.FromSeconds(30));

        // act
        manager.SetException(correlationId, new InvalidOperationException("error"));
        var completed = manager.CompletePromise(correlationId, "result");

        // assert
        Assert.False(completed);
        await Assert.ThrowsAsync<InvalidOperationException>(() => tcs.Task);
    }

    [Fact]
    public async Task SetException_Should_BeNoOp_When_CompletePromiseIsCalledAfter()
    {
        // arrange
        var manager = new DeferredResponseManager(TimeProvider.System);
        var correlationId = Guid.NewGuid().ToString();
        var tcs = manager.AddPromise(correlationId, TimeSpan.FromSeconds(30));

        // act
        manager.CompletePromise(correlationId, "result");
        manager.SetException(correlationId, new InvalidOperationException("error"));

        // assert - task should complete successfully with result
        var result = await tcs.Task;
        Assert.Equal("result", result);
    }

    [Fact]
    public async Task AddPromise_Should_CompleteSuccessfully_When_ResultIsNull()
    {
        // arrange
        var manager = new DeferredResponseManager(TimeProvider.System);
        var correlationId = Guid.NewGuid().ToString();

        // act
        var tcs = manager.AddPromise(correlationId, TimeSpan.FromSeconds(30));
        manager.CompletePromise(correlationId, null);

        // assert
        var result = await tcs.Task;
        Assert.Null(result);
    }
}
