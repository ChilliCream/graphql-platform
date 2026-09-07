using HotChocolate.Text.Json;

namespace HotChocolate.Execution.Processing;

public sealed class DeferExecutionCoordinatorTests
{
    [Fact]
    public async Task StreamBranch_Should_RemainPendingUntilItCompletes()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var streamBranchId = coordinator.RegisterStreamBranch(mainBranchId, Path.Root.Append("items"), "items");

        // act
        coordinator.EnqueueResult(CreateResult());
        await coordinator.EnqueueStreamItem(CreateResult(), streamBranchId);
        await coordinator.EnqueueStreamItem(CreateResult(), streamBranchId);
        coordinator.CompleteStream(streamBranchId);
        var results = await ReadResultsAsync(coordinator);

        // assert
        Assert.Equal(
            [
                (Pending: $"{streamBranchId}", Incremental: "", Completed: "", HasNext: true),
                (Pending: "",
                    Incremental: $"{streamBranchId},{streamBranchId}",
                    Completed: $"{streamBranchId}",
                    HasNext: false)
            ],
            DescribePayloads(results));
    }

    [Fact]
    public async Task HasNext_Should_RemainTrueUntilDeferredAndStreamBranchesComplete()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var streamBranchId = coordinator.RegisterStreamBranch(mainBranchId, Path.Root.Append("items"), null);
        var deferBranchId = coordinator.Branch(
            mainBranchId,
            Path.Root.Append("details"),
            new DeferUsage(null, null, 0));
        var initialResult = CreateResult();

        // act
        coordinator.EnqueueResult(initialResult);
        coordinator.CompleteStream(streamBranchId);
        await coordinator.EnqueueResult(CreateResult(), deferBranchId);
        var results = await ReadResultsAsync(coordinator);

        // assert
        Assert.Equal(
            [
                (Pending: $"{streamBranchId},{deferBranchId}", Incremental: "", Completed: "", HasNext: true),
                (Pending: "",
                    Incremental: $"{deferBranchId}",
                    Completed: $"{streamBranchId},{deferBranchId}",
                    HasNext: false)
            ],
            DescribePayloads(results));
    }

    [Fact]
    public void CompleteStreamBeforeReveal_Should_CompleteZeroItemStream()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var streamBranchId = coordinator.RegisterStreamBranch(mainBranchId, Path.Root.Append("items"), null);
        var error = ErrorBuilder.New().SetMessage("boom").Build();
        var initialResult = CreateResult();

        // act
        coordinator.CompleteStream(streamBranchId, [error]);
        coordinator.EnqueueResult(initialResult);

        // assert
        Assert.Equal([streamBranchId], initialResult.Pending.Select(t => t.Id));
        var completed = Assert.Single(initialResult.Completed);
        Assert.Same(error, Assert.Single(completed.Errors!));
        Assert.False(initialResult.HasNext);
    }

    [Fact]
    public async Task CompleteStreamBeforeReveal_Should_FlushBufferedItemsAndCompleteStream()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var streamBranchId = coordinator.RegisterStreamBranch(mainBranchId, Path.Root.Append("items"), null);
        var error = ErrorBuilder.New().SetMessage("boom").Build();
        var initialResult = CreateResult();

        // act
        await coordinator.EnqueueStreamItem(CreateResult(), streamBranchId);
        await coordinator.EnqueueStreamItem(CreateResult(), streamBranchId);
        coordinator.CompleteStream(streamBranchId, [error]);
        coordinator.EnqueueResult(initialResult);

        // assert
        Assert.Equal([streamBranchId], initialResult.Pending.Select(t => t.Id));
        Assert.Equal([streamBranchId, streamBranchId], initialResult.Incremental.Select(t => t.Id));
        var completed = Assert.Single(initialResult.Completed);
        Assert.Same(error, Assert.Single(completed.Errors!));
        Assert.False(initialResult.HasNext);
    }

    [Fact]
    public async Task NestedDeferredBranch_Should_BeAnnouncedWithTheRevealingStreamItem()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var streamBranchId = coordinator.RegisterStreamBranch(mainBranchId, Path.Root.Append("items"), null);
        var initialResult = CreateResult();
        coordinator.EnqueueResult(initialResult);
        var deferredBranchId = coordinator.Branch(
            streamBranchId,
            Path.Root.Append("items").Append(0).Append("details"),
            new DeferUsage(null, null, 0));

        // act
        await coordinator.EnqueueResult(CreateResult(), deferredBranchId);

        // assert
        Assert.Equal([streamBranchId], initialResult.Pending.Select(t => t.Id));

        // act
        await coordinator.EnqueueStreamItem(CreateResult(), streamBranchId);
        coordinator.CompleteStream(streamBranchId);
        var results = await ReadResultsAsync(coordinator);

        // assert
        Assert.Equal(
            [
                (Pending: $"{streamBranchId}", Incremental: "", Completed: "", HasNext: true),
                (Pending: $"{deferredBranchId}",
                    Incremental: $"{streamBranchId},{deferredBranchId}",
                    Completed: $"{deferredBranchId},{streamBranchId}",
                    HasNext: false)
            ],
            DescribePayloads(results));
    }

    [Fact]
    public async Task ReadyStreamItems_Should_CoalesceIntoSeparateIncrementalPayload()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var streamBranchId = coordinator.RegisterStreamBranch(mainBranchId, Path.Root.Append("items"), null);

        // act
        coordinator.EnqueueResult(CreateResult());
        await coordinator.EnqueueStreamItem(CreateResult(), streamBranchId);
        await coordinator.EnqueueStreamItem(CreateResult(), streamBranchId);
        coordinator.CompleteStream(streamBranchId);
        var results = await ReadResultsAsync(coordinator);

        // assert
        Assert.Equal(
            [
                (Pending: $"{streamBranchId}", Incremental: "", Completed: "", HasNext: true),
                (Pending: "",
                    Incremental: $"{streamBranchId},{streamBranchId}",
                    Completed: $"{streamBranchId}",
                    HasNext: false)
            ],
            DescribePayloads(results));
    }

    [Fact]
    public async Task AbortBranches_Should_CompleteAnnouncedBranchesAndDropUnannouncedBranches()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var streamBranchId = coordinator.RegisterStreamBranch(mainBranchId, Path.Root.Append("items"), null);
        var initialResult = CreateResult();
        coordinator.EnqueueResult(initialResult);
        _ = coordinator.RegisterStreamBranch(streamBranchId, Path.Root.Append("items").Append(0), null);
        var error = ErrorBuilder.New().SetMessage("boom").Build();

        // act
        await coordinator.AbortBranchesAsync(Path.Root, [error]);
        var results = await ReadResultsAsync(coordinator);

        // assert
        Assert.Equal(
            [
                (Pending: $"{streamBranchId}", Incremental: "", Completed: "", HasNext: true),
                (Pending: "", Incremental: "", Completed: $"{streamBranchId}", HasNext: false)
            ],
            DescribePayloads(results));
        var completed = Assert.Single(results[1].Completed);
        Assert.Equal(streamBranchId, completed.Id);
        Assert.Same(error, Assert.Single(completed.Errors!));
    }

    [Fact]
    public async Task CompleteStream_Should_DropUnannouncedNestedBranches()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var streamBranchId = coordinator.RegisterStreamBranch(mainBranchId, Path.Root.Append("items"), null);
        var initialResult = CreateResult();
        coordinator.EnqueueResult(initialResult);
        var deferredBranchId = coordinator.Branch(
            streamBranchId,
            Path.Root.Append("items").Append(0).Append("details"),
            new DeferUsage(null, null, 0));
        var nestedResult = CreateResult(new CountingMemoryHolder());
        await coordinator.EnqueueResult(nestedResult, deferredBranchId);

        // act
        coordinator.CompleteStream(streamBranchId);
        var results = await ReadResultsAsync(coordinator);
        await initialResult.DisposeAsync();
        await results[1].DisposeAsync();

        // assert
        Assert.Equal(
            [
                (Pending: $"{streamBranchId}", Incremental: "", Completed: "", HasNext: true),
                (Pending: "", Incremental: "", Completed: $"{streamBranchId}", HasNext: false)
            ],
            DescribePayloads(results));
    }

    [Fact]
    public async Task AbortBranches_Should_TransferCleanupToPayload()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var initialResult = CreateResult();
        coordinator.EnqueueResult(initialResult);
        var streamBranchId = coordinator.RegisterStreamBranch(mainBranchId, Path.Root.Append("items"), null);
        var deferBranchId = coordinator.Branch(mainBranchId, Path.Root.Append("details"), new DeferUsage(null, null, 0));
        var deferredHolder = new CountingMemoryHolder();
        var streamHolder = new CountingMemoryHolder();
        await coordinator.EnqueueResult(CreateResult(deferredHolder), deferBranchId);
        await coordinator.EnqueueStreamItem(CreateResult(streamHolder), streamBranchId);

        // act
        await coordinator.AbortBranchesAsync(Path.Root, [ErrorBuilder.New().SetMessage("boom").Build()]);

        // assert
        Assert.Equal(0, deferredHolder.DisposeCount);
        Assert.Equal(0, streamHolder.DisposeCount);

        // act
        await initialResult.DisposeAsync();
        await coordinator.ResetAsync();

        // assert
        Assert.Equal(1, deferredHolder.DisposeCount);
        Assert.Equal(1, streamHolder.DisposeCount);
    }

    [Fact]
    public async Task EnqueueResult_Should_DiscardLateResultWhenAnnouncedBranchWasAborted()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var deferBranchId = coordinator.Branch(mainBranchId, Path.Root.Append("details"), new DeferUsage(null, null, 0));
        var initialResult = CreateResult();
        var lateHolder = new CountingMemoryHolder();
        var lateResult = CreateResult(lateHolder);
        coordinator.EnqueueResult(initialResult);

        // act
        await coordinator.AbortBranchesAsync(Path.Root, [ErrorBuilder.New().SetMessage("boom").Build()]);
        var results = await ReadResultsAsync(coordinator);
        await coordinator.EnqueueResult(lateResult, deferBranchId);
        await initialResult.DisposeAsync();
        await coordinator.ResetAsync();

        // assert
        Assert.Equal([deferBranchId], initialResult.Pending.Select(t => t.Id));
        Assert.Empty(initialResult.Incremental);
        Assert.Equal([deferBranchId], results[1].Completed.Select(t => t.Id));
        Assert.False(results[1].HasNext);
        Assert.Equal(1, lateHolder.DisposeCount);
    }

    [Fact]
    public async Task EnqueueResult_Should_DiscardLateResultWhenUnannouncedBranchWasAborted()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var parentBranchId = coordinator.Branch(mainBranchId, Path.Root.Append("details"), new DeferUsage(null, null, 0));
        var initialResult = CreateResult();
        coordinator.EnqueueResult(initialResult);
        var deferBranchId = coordinator.Branch(
            parentBranchId,
            Path.Root.Append("details").Append("more"),
            new DeferUsage(null, null, 0));
        var lateHolder = new CountingMemoryHolder();
        var lateResult = CreateResult(lateHolder);

        // act
        await coordinator.AbortBranchesAsync(Path.Root.Append("details"), [ErrorBuilder.New().SetMessage("boom").Build()]);
        var results = await ReadResultsAsync(coordinator);
        await coordinator.EnqueueResult(lateResult, deferBranchId);
        await initialResult.DisposeAsync();
        await coordinator.ResetAsync();

        // assert
        Assert.Equal([parentBranchId], initialResult.Pending.Select(t => t.Id));
        Assert.Empty(initialResult.Incremental);
        Assert.Equal([parentBranchId], results[1].Completed.Select(t => t.Id));
        Assert.False(results[1].HasNext);
        Assert.Equal(1, lateHolder.DisposeCount);
    }

    [Fact]
    public async Task EnqueueResult_Should_AwaitRejectedCleanupWithoutHoldingCoordinatorLock()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var deferBranchId = coordinator.Branch(mainBranchId, Path.Root.Append("details"), new DeferUsage(null, null, 0));
        var initialResult = CreateResult();
        var lateCleanup = new BlockingAsyncCleanup();
        var lateResult = CreateResult();
        lateResult.RegisterForCleanup(lateCleanup);
        coordinator.EnqueueResult(initialResult);
        await coordinator.AbortBranchesAsync(Path.Root, [ErrorBuilder.New().SetMessage("boom").Build()]);

        // act
        var enqueueResult = coordinator.EnqueueResult(lateResult, deferBranchId).AsTask();
        await lateCleanup.WaitForStartAsync(TestContext.Current.CancellationToken);
        var overlappingEnqueueResult = coordinator.EnqueueResult(lateResult, deferBranchId).AsTask();
        var abortTask = coordinator.AbortBranchesAsync(Path.Root, [ErrorBuilder.New().SetMessage("boom").Build()]);

        // assert
        Assert.True(abortTask.IsCompletedSuccessfully);

        // act
        lateCleanup.Release();
        await Task.WhenAll(enqueueResult, overlappingEnqueueResult);
        await initialResult.DisposeAsync();
        await coordinator.ResetAsync();

        // assert
        Assert.Equal(1, lateCleanup.DisposeCount);
    }

    [Fact]
    public async Task EnqueueResult_Should_SurfaceRejectedCleanupFailure()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var deferBranchId = coordinator.Branch(mainBranchId, Path.Root.Append("details"), new DeferUsage(null, null, 0));
        var initialResult = CreateResult();
        var lateCleanup = new BlockingAsyncCleanup(throwOnDispose: true);
        var lateResult = CreateResult();
        lateResult.RegisterForCleanup(lateCleanup);
        coordinator.EnqueueResult(initialResult);
        await coordinator.AbortBranchesAsync(Path.Root, [ErrorBuilder.New().SetMessage("boom").Build()]);

        // act
        var enqueueResult = coordinator.EnqueueResult(lateResult, deferBranchId).AsTask();
        await lateCleanup.WaitForStartAsync(TestContext.Current.CancellationToken);
        lateCleanup.Release();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => enqueueResult);
        await initialResult.DisposeAsync();
        await coordinator.ResetAsync();

        // assert
        Assert.Equal("cleanup failed", exception.Message);
        Assert.Equal(1, lateCleanup.DisposeCount);
    }

    [Fact]
    public async Task EnqueueResult_Should_DeliverSeparateIncrementalPayload_When_InitialResultIsErrorsOnly()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var deferBranchId = coordinator.Branch(mainBranchId, Path.Root.Append("details"), new DeferUsage(null, null, 0));
        var initialResult = new OperationResult([ErrorBuilder.New().SetMessage("boom").Build()]);

        // act
        coordinator.EnqueueResult(initialResult);
        await coordinator.EnqueueResult(CreateResult(), deferBranchId);
        var results = await ReadResultsAsync(coordinator);

        // assert
        Assert.Equal(
            [
                (Pending: $"{deferBranchId}", Incremental: "", Completed: "", HasNext: true),
                (Pending: "", Incremental: $"{deferBranchId}", Completed: $"{deferBranchId}", HasNext: false)
            ],
            DescribePayloads(results));
        Assert.Null(results[0].Data);
    }

    private static DeferExecutionCoordinator CreateCoordinator(out int mainBranchId)
    {
        var branchTracker = new BranchTracker();
        mainBranchId = branchTracker.CreateNewBranchId();
        var coordinator = new DeferExecutionCoordinator();
        coordinator.Initialize(branchTracker, mainBranchId);
        return coordinator;
    }

    private static async Task<List<OperationResult>> ReadResultsAsync(DeferExecutionCoordinator coordinator)
    {
        var results = new List<OperationResult>();

        await foreach (var payload in coordinator.ReadResultsAsync(TestContext.Current.CancellationToken))
        {
            results.Add(payload);
        }

        return results;
    }

    private static IEnumerable<(string Pending, string Incremental, string Completed, bool? HasNext)>
        DescribePayloads(IEnumerable<OperationResult> results)
        => results.Select(
            t => (string.Join(",", t.Pending.Select(p => p.Id)),
                string.Join(",", t.Incremental.Select(i => i.Id)),
                string.Join(",", t.Completed.Select(c => c.Id)),
                t.HasNext));

    private static OperationResult CreateResult(IDisposable? memoryHolder = null)
        => new(
            new OperationResultData(
                new object(),
                isValueNull: false,
                EmptyFormatter.Instance,
                memoryHolder));

    private sealed class CountingMemoryHolder : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }

    private sealed class BlockingAsyncCleanup(bool throwOnDispose = false) : IAsyncDisposable
    {
        private readonly TaskCompletionSource<bool> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int DisposeCount { get; private set; }

        public async ValueTask DisposeAsync()
        {
            DisposeCount++;
            _started.TrySetResult(true);
            await _release.Task.ConfigureAwait(false);

            if (throwOnDispose)
            {
                throw new InvalidOperationException("cleanup failed");
            }
        }

        public Task WaitForStartAsync(CancellationToken cancellationToken)
            => _started.Task.WaitAsync(cancellationToken);

        public void Release() => _release.TrySetResult(true);
    }

    private sealed class EmptyFormatter : IRawJsonFormatter
    {
        public static EmptyFormatter Instance { get; } = new();

        public void WriteDataTo(JsonWriter jsonWriter)
        {
        }
    }
}
