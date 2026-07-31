using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class GatewaySchemaValidationWorkerTests
{
    [Fact]
    public async Task Enqueue_Should_ReactivateCachedReport_When_KnownHashReturns()
    {
        // arrange
        using var stopping = new CancellationTokenSource();
        var notifier = new RecordingNotifier();
        var calls = 0;
        var worker = CreateWorker(
            (_, _) => Task.FromResult(
                ++calls == 1
                    ? Violations("a", "request-a")
                    : Passed("b", "request-b")),
            notifier,
            stopping.Token);

        // act
        worker.Enqueue(new GatewaySchemaValidationRequest([1], "a"));
        await WaitForReportAsync(worker, "request-a");
        worker.Enqueue(new GatewaySchemaValidationRequest([2], "b"));
        await WaitForReportAsync(worker, "request-b");
        worker.Enqueue(new GatewaySchemaValidationRequest([1], "a"));
        await WaitForReportAsync(worker, "request-a");
        await WaitForAsync(() => notifier.Transitions.Count == 3);
        await StopWorkerAsync(stopping, worker);

        // assert
        Assert.Equal(2, calls);
        Assert.Equal(
            ["violations", "restored", "violations"],
            notifier.Transitions);
    }

    [Fact]
    public async Task Enqueue_Should_DiscardStaleResult_When_NewerGenerationArrives()
    {
        // arrange
        using var stopping = new CancellationTokenSource();
        var notifier = new RecordingNotifier();
        var firstResult = new TaskCompletionSource<NitroSchemaValidationReport>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var firstCanceled = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var worker = CreateWorker(
            (request, cancellationToken) =>
            {
                calls++;
                if (request.SchemaHash != "a")
                {
                    return Task.FromResult(Passed("b", "request-b"));
                }

                cancellationToken.Register(() => firstCanceled.TrySetResult());
                return firstResult.Task;
            },
            notifier,
            stopping.Token);

        // act
        worker.Enqueue(new GatewaySchemaValidationRequest([1], "a"));
        await WaitForAsync(() => calls == 1);
        worker.Enqueue(new GatewaySchemaValidationRequest([2], "b"));
        await firstCanceled.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);
        firstResult.SetResult(Violations("a", "request-a"));
        await WaitForReportAsync(worker, "request-b");
        await StopWorkerAsync(stopping, worker);

        // assert
        Assert.Equal(2, calls);
        Assert.Empty(notifier.Transitions);
        Assert.Equal(NitroSchemaValidationStatus.Passed, worker.LatestReport!.Status);
    }

    [Fact]
    public async Task Enqueue_Should_CoalescePendingDuplicate_When_HashIsTheSame()
    {
        // arrange
        using var stopping = new CancellationTokenSource();
        var result = new TaskCompletionSource<NitroSchemaValidationReport>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var worker = CreateWorker(
            (_, _) =>
            {
                calls++;
                return result.Task;
            },
            new RecordingNotifier(),
            stopping.Token);

        // act
        var first = worker.Enqueue(new GatewaySchemaValidationRequest([1], "a"));
        await WaitForAsync(() => calls == 1);
        var duplicate = worker.Enqueue(new GatewaySchemaValidationRequest([1], "a"));
        result.SetResult(Passed("a", "request-a"));
        await WaitForReportAsync(worker, "request-a");
        await StopWorkerAsync(stopping, worker);

        // assert
        Assert.True(first);
        Assert.False(duplicate);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Enqueue_Should_DeduplicateSameFingerprintOnlyForAdjacentViolations()
    {
        // arrange
        using var stopping = new CancellationTokenSource();
        var notifier = new RecordingNotifier();
        var reports = new Queue<NitroSchemaValidationReport>(
        [
            Violations("a", "request-a"),
            Violations("b", "request-b"),
            Violations("c", "request-c", code: "HC002")
        ]);
        var worker = CreateWorker(
            (_, _) => Task.FromResult(reports.Dequeue()),
            notifier,
            stopping.Token);

        // act
        worker.Enqueue(new GatewaySchemaValidationRequest([1], "a"));
        await WaitForReportAsync(worker, "request-a");
        worker.Enqueue(new GatewaySchemaValidationRequest([2], "b"));
        await WaitForReportAsync(worker, "request-b");
        worker.Enqueue(new GatewaySchemaValidationRequest([3], "c"));
        await WaitForReportAsync(worker, "request-c");
        await WaitForAsync(() => notifier.Transitions.Count == 2);
        await StopWorkerAsync(stopping, worker);

        // assert
        Assert.Equal(["violations", "violations"], notifier.Transitions);
    }

    [Fact]
    public async Task Enqueue_Should_NotNotify_When_NoViolationOccurred()
    {
        // arrange
        using var stopping = new CancellationTokenSource();
        var notifier = new RecordingNotifier();
        var reports = new Queue<NitroSchemaValidationReport>(
        [
            Passed("a", "request-a"),
            NitroSchemaValidationReport.Unavailable(
                "b",
                "network",
                DateTimeOffset.UtcNow,
                "request-b"),
            Passed("c", "request-c")
        ]);
        var worker = CreateWorker(
            (_, _) => Task.FromResult(reports.Dequeue()),
            notifier,
            stopping.Token);

        // act
        worker.Enqueue(new GatewaySchemaValidationRequest([1], "a"));
        await WaitForReportAsync(worker, "request-a");
        worker.Enqueue(new GatewaySchemaValidationRequest([2], "b"));
        await WaitForReportAsync(worker, "request-b");
        worker.Enqueue(new GatewaySchemaValidationRequest([3], "c"));
        await WaitForReportAsync(worker, "request-c");
        await StopWorkerAsync(stopping, worker);

        // assert
        Assert.Empty(notifier.Transitions);
    }

    [Fact]
    public async Task Enqueue_Should_NotifyAgain_When_UnavailableSeparatesSameViolations()
    {
        // arrange
        using var stopping = new CancellationTokenSource();
        var notifier = new RecordingNotifier();
        var reports = new Queue<NitroSchemaValidationReport>(
        [
            Violations("a", "request-a"),
            NitroSchemaValidationReport.Unavailable(
                "b",
                "network",
                DateTimeOffset.UtcNow,
                "request-b"),
            Violations("c", "request-c")
        ]);
        var worker = CreateWorker(
            (_, _) => Task.FromResult(reports.Dequeue()),
            notifier,
            stopping.Token);

        // act
        worker.Enqueue(new GatewaySchemaValidationRequest([1], "a"));
        await WaitForReportAsync(worker, "request-a");
        worker.Enqueue(new GatewaySchemaValidationRequest([2], "b"));
        await WaitForReportAsync(worker, "request-b");
        worker.Enqueue(new GatewaySchemaValidationRequest([3], "c"));
        await WaitForReportAsync(worker, "request-c");
        await WaitForAsync(() => notifier.Transitions.Count == 2);
        await StopWorkerAsync(stopping, worker);

        // assert
        Assert.Equal(["violations", "violations"], notifier.Transitions);
    }

    [Fact]
    public async Task Enqueue_Should_LogUnavailableReasonOncePerDistinctReason()
    {
        // arrange
        using var stopping = new CancellationTokenSource();
        var resourceLogger = new RecordingLogger<GatewaySchemaValidationWorker>();
        var reports = new Queue<NitroSchemaValidationReport>(
        [
            NitroSchemaValidationReport.Unavailable(
                "a",
                "network: token-a",
                DateTimeOffset.UtcNow,
                "request-a"),
            NitroSchemaValidationReport.Unavailable(
                "b",
                "network: token-b",
                DateTimeOffset.UtcNow,
                "request-b"),
            NitroSchemaValidationReport.Unavailable(
                "c",
                "authentication: token-c",
                DateTimeOffset.UtcNow,
                "request-c")
        ]);
        var worker = CreateWorker(
            (_, _) => Task.FromResult(reports.Dequeue()),
            new RecordingNotifier(),
            stopping.Token,
            resourceLogger);

        // act
        worker.Enqueue(new GatewaySchemaValidationRequest([1], "a"));
        await WaitForReportAsync(worker, "request-a");
        worker.Enqueue(new GatewaySchemaValidationRequest([2], "b"));
        await WaitForReportAsync(worker, "request-b");
        worker.Enqueue(new GatewaySchemaValidationRequest([3], "c"));
        await WaitForReportAsync(worker, "request-c");
        await WaitForAsync(
            () => resourceLogger.Entries.Count(entry => entry.Level is LogLevel.Warning) == 2);
        await StopWorkerAsync(stopping, worker);

        // assert
        resourceLogger.Entries
            .Where(entry => entry.Level is LogLevel.Warning)
            .Select(entry => entry.Message)
            .MatchInlineSnapshots(
            [
                "Nitro schema validation is unavailable for gateway: network",
                "Nitro schema validation is unavailable for gateway: authentication"
            ]);
    }

    [Fact]
    public async Task RunAsync_Should_CancelActiveValidation_When_ApplicationStops()
    {
        // arrange
        using var stopping = new CancellationTokenSource();
        var started = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var worker = CreateWorker(
            async (_, cancellationToken) =>
            {
                started.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    return Passed("a", "request-a");
                }
                catch (OperationCanceledException)
                {
                    canceled.TrySetResult();
                    throw;
                }
            },
            new RecordingNotifier(),
            stopping.Token);

        // act
        worker.Enqueue(new GatewaySchemaValidationRequest([1], "a"));
        await started.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);
        await stopping.CancelAsync();
        await canceled.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);
        await worker.Completion.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(canceled.Task.IsCompletedSuccessfully);
        Assert.Null(worker.LatestReport);
    }

    private static GatewaySchemaValidationWorker CreateWorker(
        Func<GatewaySchemaValidationRequest, CancellationToken, Task<NitroSchemaValidationReport>>
            validate,
        INitroSchemaValidationNotifier notifier,
        CancellationToken stoppingToken,
        ILogger? resourceLogger = null)
        => new(
            "gateway",
            "production",
            validate,
            resourceLogger ?? NullLogger.Instance,
            notifier,
            stoppingToken,
            NullLogger<GatewaySchemaValidationWorker>.Instance);

    private static NitroSchemaValidationReport Passed(string hash, string requestId)
        => NitroSchemaValidationReport.Passed(hash, requestId, DateTimeOffset.UtcNow);

    private static NitroSchemaValidationReport Violations(
        string hash,
        string requestId,
        string code = "HC001")
        => new(
            NitroSchemaValidationStatus.Violations,
            hash,
            requestId,
            [
                new NitroClientContractViolation(
                    "client",
                    "Client",
                    [
                        new NitroOperationContractViolation(
                            "operation",
                            ["production"],
                            [
                                new NitroSchemaValidationFinding(
                                    "Client contract violations",
                                    "PersistedQueryValidationError",
                                    "Field does not exist.",
                                    code,
                                    Path: "query.field",
                                    Line: 1,
                                    Column: 2)
                            ])
                    ])
            ],
            [],
            null,
            DateTimeOffset.UtcNow);

    private static async Task WaitForReportAsync(
        GatewaySchemaValidationWorker worker,
        string requestId)
        => await WaitForAsync(
            () => string.Equals(
                worker.LatestReport?.RequestId,
                requestId,
                StringComparison.Ordinal));

    private static async Task WaitForAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private static async Task StopWorkerAsync(
        CancellationTokenSource stopping,
        GatewaySchemaValidationWorker worker)
    {
        await stopping.CancelAsync();
        await worker.Completion.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);
    }

    private sealed class RecordingNotifier : INitroSchemaValidationNotifier
    {
        private readonly ConcurrentQueue<string> _transitions = [];

        public IReadOnlyList<string> Transitions => [.. _transitions];

        public void NotifyViolations(string message) => _transitions.Enqueue("violations");

        public void NotifyRestored(string message) => _transitions.Enqueue("restored");
    }
}
