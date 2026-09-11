using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotChocolate.Fusion.Policies.Rego;

public sealed class RegoDataAggregatorTests
{
    private static readonly byte[] s_farData = "{\"far\":{}}"u8.ToArray();

    [Fact]
    public void Ctor_Should_Throw_When_ProviderNameIsDuplicated()
    {
        // arrange
        var registrations = new[]
        {
            Registration("dup", new InMemoryRegoDataProvider("{}")),
            Registration("dup", new InMemoryRegoDataProvider("{}"))
        };

        // act
        void Act() => new RegoDataAggregator(
            registrations,
            EmptyServiceProvider.Instance,
            new TestDiagnosticEvents(),
            NullLogger.Instance);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void TryBuildMergedData_Should_ReturnNotReady_When_AProviderHasNotLoadedYet()
    {
        // arrange
        var provider = new InMemoryRegoDataProvider("""{"orders":{}}""");
        provider.Handler = _ => new ValueTask<RegoDataSnapshot>(
            new TaskCompletionSource<RegoDataSnapshot>().Task);
        var aggregator = CreateAggregator(Registration("orders", provider));

        // act
        aggregator.Start();
        var status = aggregator.TryBuildMergedData(s_farData, out _, out _);

        // assert
        Assert.Equal(RegoDataMergeStatus.NotReady, status);
    }

    [Fact]
    public async Task TryBuildMergedData_Should_ReturnReady_When_AllProvidersLoadSuccessfully()
    {
        // arrange
        var provider = new InMemoryRegoDataProvider("""{"orders":{"open":true}}""");
        var aggregator = CreateAggregator(Registration("orders", provider));

        // act
        aggregator.Start();
        await WaitUntilAsync(() => aggregator.TryBuildMergedData(s_farData, out _, out _)
            == RegoDataMergeStatus.Ready);
        var status = aggregator.TryBuildMergedData(s_farData, out var attempt, out var error);

        // assert
        Assert.Equal(RegoDataMergeStatus.Ready, status);
        Assert.Null(error);
        Assert.Contains(
            "orders",
            System.Text.Encoding.UTF8.GetString(attempt!.MergedData),
            StringComparison.Ordinal);
    }

    [Fact]
    public void TryBuildMergedData_Should_ReturnFailed_When_ProviderDataCollidesWithFarData()
    {
        // arrange
        var farData = "{\"orders\":{}}"u8.ToArray();
        var provider = new InMemoryRegoDataProvider("""{"orders":{"open":true}}""");
        var aggregator = CreateAggregator(Registration("orders", provider));

        // act
        aggregator.Start();
        RegoDataMergeStatus status = default;
        Exception? error = null;
        Spin(() =>
        {
            status = aggregator.TryBuildMergedData(farData, out _, out error);
            return status != RegoDataMergeStatus.NotReady;
        });

        // assert
        Assert.Equal(RegoDataMergeStatus.Failed, status);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryBuildMergedData_Should_AttributeFailureToAProvider_When_MultipleProvidersHaveNoLastGoodAndCollide()
    {
        // arrange: two providers, neither with a last-good snapshot yet, whose very first
        // snapshots collide with each other rather than with the (empty) FAR data.
        var providerA = new InMemoryRegoDataProvider("""{"shared":{"x":1}}""");
        var providerB = new InMemoryRegoDataProvider("""{"shared":{"x":2}}""");
        var aggregator = CreateAggregator(
            [
                Registration("a", providerA),
                Registration("b", providerB)
            ]);

        // act
        aggregator.Start();
        RegoDataMergeStatus status = default;
        Exception? error = null;
        Spin(() =>
        {
            status = aggregator.TryBuildMergedData(s_farData, out _, out error);
            return status != RegoDataMergeStatus.NotReady;
        });

        // assert: the failure is attributed to one of the colliding no-last-good providers (F2m),
        // not surfaced as a bare, unattributed merge exception.
        Assert.Equal(RegoDataMergeStatus.Failed, status);
        var providerError = Assert.IsType<RegoDataProviderException>(error);
        Assert.Contains(providerError.ProviderName, new[] { "a", "b" });
    }

    [Fact]
    public void TryBuildMergedData_Should_PromoteForcedCandidate_When_RejectedCandidateMergesCleanlyInFinalAttempt()
    {
        // arrange: two providers whose initial snapshots do not collide.
        var providerX = new InMemoryRegoDataProvider("""{"a":1}""");
        var providerY = new InMemoryRegoDataProvider("""{"k":1}""");
        var aggregator = CreateAggregator(
            [
                Registration("x", providerX),
                Registration("y", providerY)
            ]);
        aggregator.Start();
        RegoDataMergeStatus firstStatus = default;
        RegoDataAggregator.RegoDataMergeAttempt? firstAttempt = null;
        Spin(() =>
        {
            firstStatus = aggregator.TryBuildMergedData(s_farData, out firstAttempt, out _);
            return firstStatus != RegoDataMergeStatus.NotReady;
        });

        // act: X republishes data that collides with Y's still-pending candidate BEFORE the first
        // attempt is committed, so committing it skips X as moved-on (X stays pending with no
        // last-good) while Y's unchanged candidate commits normally. Y then republishes data that
        // no longer collides with X's still-pending, still-rejected candidate.
        providerX.Publish("""{"k":2}""", "v2");
        firstAttempt!.Commit();
        providerY.Publish("""{"m":1}""", "v2");

        var status = aggregator.TryBuildMergedData(s_farData, out var attempt, out var error);

        // assert: X's rejected, no-last-good candidate is used as the merge fallback and, since it
        // merges cleanly this time, is promoted alongside Y instead of being served silently
        // without ever becoming X's committed state (F2m).
        Assert.Equal(RegoDataMergeStatus.Ready, status);
        Assert.Null(error);
        Assert.Contains("\"k\":2", System.Text.Encoding.UTF8.GetString(attempt!.MergedData), StringComparison.Ordinal);
        attempt.Commit();
        Assert.Equal("v2", GetProviderSnapshotVersion(aggregator, "x"));
    }

    [Fact]
    public void TryBuildMergedData_Should_LogSizeWarningWithoutRejecting_When_MergedDataExceedsThreshold()
    {
        // arrange
        var largeValue = new string('a', 17 * 1024 * 1024);
        var provider = new InMemoryRegoDataProvider($$"""{"blob":"{{largeValue}}"}""");
        var logger = new CapturingLogger();
        var aggregator = CreateAggregator([Registration("blob", provider)], logger: logger);

        // act
        // The default in-memory provider loads synchronously, so a single call after Start
        // already observes the merged, oversized document.
        aggregator.Start();
        var status = aggregator.TryBuildMergedData(s_farData, out var attempt, out var error);

        // assert
        Assert.Equal(RegoDataMergeStatus.Ready, status);
        Assert.Null(error);
        Assert.NotNull(attempt);
        Assert.Single(logger.Warnings);
    }

    [Fact]
    public async Task Start_Should_ReportProviderExceptionAndKeepLastGood_When_ProviderThrowsOnRefresh()
    {
        // arrange
        var provider = new InMemoryRegoDataProvider("""{"orders":{}}""");
        var diagnostics = new TestDiagnosticEvents();
        var aggregator = CreateAggregator([Registration("orders", provider)], diagnostics);
        aggregator.Start();
        await WaitUntilAsync(() => aggregator.TryBuildMergedData(s_farData, out _, out _)
            == RegoDataMergeStatus.Ready);

        // act
        provider.Handler = _ => throw new InvalidOperationException("boom");
        provider.Publish("""{"orders":{"stale":true}}""", "v2");
        await WaitUntilAsync(() => diagnostics.UpdateErrors.Count > 0);

        // assert
        var reported = Assert.Single(diagnostics.UpdateErrors);
        var providerError = Assert.IsType<RegoDataProviderException>(reported);
        Assert.Equal("orders", providerError.ProviderName);
        aggregator.TryBuildMergedData(s_farData, out var attempt, out _);
        var merged = System.Text.Encoding.UTF8.GetString(attempt!.MergedData);
        Assert.Contains("orders", merged, StringComparison.Ordinal);
        Assert.DoesNotContain("stale", merged, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Start_Should_KeepLastGoodWithoutDiagnostics_When_ProviderRefreshTimesOut()
    {
        // arrange
        var provider = new InMemoryRegoDataProvider("""{"orders":{}}""");
        var diagnostics = new TestDiagnosticEvents();
        var aggregator = CreateAggregator(
            [Registration("orders", provider)],
            diagnostics,
            refreshTimeout: TimeSpan.FromMilliseconds(50));
        aggregator.Start();
        await WaitUntilAsync(() => aggregator.TryBuildMergedData(s_farData, out _, out _)
            == RegoDataMergeStatus.Ready);

        // act
        provider.Handler = async token =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return new RegoDataSnapshot("""{"orders":{}}"""u8, "unreachable");
        };
        provider.Publish("""{"orders":{"stale":true}}""", "v2");
        await Task.Delay(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(diagnostics.UpdateErrors);
        aggregator.TryBuildMergedData(s_farData, out var attempt, out _);
        Assert.DoesNotContain(
            "stale",
            System.Text.Encoding.UTF8.GetString(attempt!.MergedData),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Start_Should_CoalesceConcurrentRefreshes_When_ChangeTokenFiresWhileRefreshIsInFlight()
    {
        // arrange
        var gate = new TaskCompletionSource<RegoDataSnapshot>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new InMemoryRegoDataProvider("""{"orders":{}}""");
        provider.Handler = _ => new ValueTask<RegoDataSnapshot>(gate.Task);
        var aggregator = CreateAggregator(Registration("orders", provider));

        // act
        aggregator.Start();
        Assert.Equal(1, provider.CallCount);

        // Fire the change token twice while the first refresh is still blocked in flight.
        provider.Publish("""{"orders":{"a":1}}""", "v2");
        provider.Publish("""{"orders":{"a":2}}""", "v3");

        gate.SetResult(new RegoDataSnapshot("""{"orders":{"a":0}}"""u8, "v1"));
        await WaitUntilAsync(() => provider.CallCount == 2);
        await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);

        // assert
        // Two change-token signals while a refresh was in flight collapse into a single follow-up
        // refresh, not two: one call for the initial load, one coalesced retry.
        Assert.Equal(2, provider.CallCount);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeFactoryCreatedProvider_When_OwnsInstanceIsTrue()
    {
        // arrange
        var provider = new DisposableInMemoryProvider("""{"orders":{}}""");
        var aggregator = CreateAggregator(
            new RegoDataProviderRegistration("orders", _ => provider, ownsInstance: true));
        aggregator.Start();
        await WaitUntilAsync(() => aggregator.TryBuildMergedData(s_farData, out _, out _)
            == RegoDataMergeStatus.Ready);

        // act
        await aggregator.DisposeAsync();

        // assert
        Assert.True(provider.Disposed);
    }

    [Fact]
    public async Task DisposeAsync_Should_NotDisposeCallerOwnedInstance_When_OwnsInstanceIsFalse()
    {
        // arrange
        var provider = new DisposableInMemoryProvider("""{"orders":{}}""");
        var aggregator = CreateAggregator(Registration("orders", provider));
        aggregator.Start();
        await WaitUntilAsync(() => aggregator.TryBuildMergedData(s_farData, out _, out _)
            == RegoDataMergeStatus.Ready);

        // act
        await aggregator.DisposeAsync();

        // assert
        Assert.False(provider.Disposed);
    }

    [Fact]
    public async Task DisposeAsync_Should_StopFurtherPublishes_When_AggregatorIsDisposed()
    {
        // arrange
        var provider = new InMemoryRegoDataProvider("""{"orders":{}}""");
        var aggregator = CreateAggregator(Registration("orders", provider));
        aggregator.Start();
        await WaitUntilAsync(() => aggregator.TryBuildMergedData(s_farData, out _, out _)
            == RegoDataMergeStatus.Ready);
        var changeCount = 0;
        aggregator.DataChanged += () => Interlocked.Increment(ref changeCount);

        // act
        await aggregator.DisposeAsync();
        provider.Publish("""{"orders":{"open":true}}""", "v2");
        await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, changeCount);
    }

    private static RegoDataAggregator CreateAggregator(
        RegoDataProviderRegistration registration,
        TestDiagnosticEvents? diagnostics = null,
        TimeSpan? refreshTimeout = null)
        => CreateAggregator([registration], diagnostics, refreshTimeout);

    private static RegoDataAggregator CreateAggregator(
        IReadOnlyList<RegoDataProviderRegistration> registrations,
        TestDiagnosticEvents? diagnostics = null,
        TimeSpan? refreshTimeout = null,
        ILogger? logger = null)
        => new(
            registrations,
            EmptyServiceProvider.Instance,
            diagnostics ?? new TestDiagnosticEvents(),
            logger ?? NullLogger.Instance,
            refreshTimeout);

    private static RegoDataProviderRegistration Registration(string name, IRegoDataProvider instance)
        => new(name, _ => instance, ownsInstance: false);

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException("The condition was not met in time.");
    }

    // Reflection is the only way to observe a provider's committed snapshot: RegoDataAggregator
    // deliberately exposes nothing about ProviderState beyond what TryBuildMergedData's own return
    // value already reveals.
    private static string? GetProviderSnapshotVersion(RegoDataAggregator aggregator, string providerName)
    {
        var providersField = typeof(RegoDataAggregator)
            .GetField("_providers", BindingFlags.NonPublic | BindingFlags.Instance)!;

        foreach (var providerState in (Array)providersField.GetValue(aggregator)!)
        {
            var providerStateType = providerState!.GetType();
            var name = (string)providerStateType
                .GetProperty("Name", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(providerState)!;

            if (!string.Equals(name, providerName, StringComparison.Ordinal))
            {
                continue;
            }

            var snapshot = (RegoDataSnapshot?)providerStateType
                .GetProperty("Snapshot", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(providerState);
            return snapshot?.Version;
        }

        throw new InvalidOperationException($"No provider named '{providerName}' was found.");
    }

    private static void Spin(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            Thread.Sleep(10);
        }

        throw new TimeoutException("The condition was not met in time.");
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static readonly EmptyServiceProvider Instance = new();

        public object? GetService(Type serviceType) => null;
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }

    private sealed class DisposableInMemoryProvider(string json) : IRegoDataProvider, IDisposable
    {
        private readonly InMemoryRegoDataProvider _inner = new(json);

        public bool Disposed { get; private set; }

        public ValueTask<RegoDataSnapshot> GetDataAsync(CancellationToken cancellationToken)
            => _inner.GetDataAsync(cancellationToken);

        public Microsoft.Extensions.Primitives.IChangeToken GetChangeToken() => _inner.GetChangeToken();

        public void Dispose() => Disposed = true;
    }
}
