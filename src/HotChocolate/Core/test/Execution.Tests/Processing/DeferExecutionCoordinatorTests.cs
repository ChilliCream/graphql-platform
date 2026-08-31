using HotChocolate.Text.Json;

namespace HotChocolate.Execution.Processing;

public sealed class DeferExecutionCoordinatorTests
{
    [Fact]
    public void StreamBranch_Should_RemainPendingUntilItCompletes()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var streamBranchId = coordinator.RegisterStreamBranch(mainBranchId, Path.Root.Append("items"), "items");
        var initialResult = CreateResult();

        // act
        coordinator.EnqueueResult(initialResult);
        coordinator.EnqueueStreamItem(CreateResult(), streamBranchId);
        coordinator.EnqueueStreamItem(CreateResult(), streamBranchId);

        // assert
        Assert.Equal([streamBranchId], initialResult.Pending.Select(t => t.Id));
        Assert.True(initialResult.HasNext);
        Assert.Equal(2, initialResult.Incremental.Count);
        Assert.Empty(initialResult.Completed);

        // act
        coordinator.CompleteStream(streamBranchId);

        // assert
        Assert.Equal([streamBranchId], initialResult.Completed.Select(t => t.Id));
        Assert.False(initialResult.HasNext);
    }

    [Fact]
    public void HasNext_Should_RemainTrueUntilDeferredAndStreamBranchesComplete()
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

        // assert
        Assert.True(initialResult.HasNext);

        // act
        coordinator.EnqueueResult(CreateResult(), deferBranchId);

        // assert
        Assert.False(initialResult.HasNext);
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
    public void NestedDeferredBranch_Should_BeAnnouncedWithTheRevealingStreamItem()
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
        coordinator.EnqueueResult(CreateResult(), deferredBranchId);

        // assert
        Assert.Equal([streamBranchId], initialResult.Pending.Select(t => t.Id));

        // act
        _ = coordinator.EnqueueStreamItem(CreateResult(), streamBranchId);

        // assert
        Assert.Equal([streamBranchId, deferredBranchId], initialResult.Pending.Select(t => t.Id).Order());
        Assert.Equal([deferredBranchId], initialResult.Completed.Select(t => t.Id));
        Assert.Equal([streamBranchId, deferredBranchId], initialResult.Incremental.Select(t => t.Id));
    }

    [Fact]
    public async Task ReadyStreamItems_Should_CoalesceIntoOnePayload()
    {
        // arrange
        var coordinator = CreateCoordinator(out var mainBranchId);
        var streamBranchId = coordinator.RegisterStreamBranch(mainBranchId, Path.Root.Append("items"), null);

        // act
        coordinator.EnqueueResult(CreateResult());
        await coordinator.EnqueueStreamItem(CreateResult(), streamBranchId);
        await coordinator.EnqueueStreamItem(CreateResult(), streamBranchId);
        coordinator.CompleteStream(streamBranchId);
        var results = new List<OperationResult>();

        await foreach (var payload in coordinator.ReadResultsAsync(TestContext.Current.CancellationToken))
        {
            results.Add(payload);
        }

        // assert
        var result = Assert.Single(results);
        Assert.Equal(2, result.Incremental.Count);
        Assert.Equal([streamBranchId], result.Completed.Select(t => t.Id));
        Assert.False(result.HasNext);
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

        // assert
        var completed = Assert.Single(initialResult.Completed);
        Assert.Equal(streamBranchId, completed.Id);
        Assert.Same(error, Assert.Single(completed.Errors!));
        Assert.False(initialResult.HasNext);
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
        coordinator.EnqueueResult(nestedResult, deferredBranchId);

        // act
        coordinator.CompleteStream(streamBranchId);
        await initialResult.DisposeAsync();

        // assert
        Assert.Equal([streamBranchId], initialResult.Pending.Select(t => t.Id));
        Assert.Equal([streamBranchId], initialResult.Completed.Select(t => t.Id));
        Assert.False(initialResult.HasNext);
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
        coordinator.EnqueueResult(CreateResult(deferredHolder), deferBranchId);
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

    private static DeferExecutionCoordinator CreateCoordinator(out int mainBranchId)
    {
        var branchTracker = new BranchTracker();
        mainBranchId = branchTracker.CreateNewBranchId();
        var coordinator = new DeferExecutionCoordinator();
        coordinator.Initialize(branchTracker, mainBranchId);
        return coordinator;
    }

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

    private sealed class EmptyFormatter : IRawJsonFormatter
    {
        public static EmptyFormatter Instance { get; } = new();

        public void WriteDataTo(JsonWriter jsonWriter)
        {
        }
    }
}
