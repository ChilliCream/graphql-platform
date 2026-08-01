using System.Collections.Concurrent;
using System.Net;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroSeedUpdateMonitorTests : IAsyncLifetime
{
    private const string ApiId = "QXBpCmdhdGV3YXk";
    private const string Stage = "production";
    private readonly NitroTestDirectory _directory = new();
    private byte[] _initialArchive = null!;
    private byte[] _updatedArchive = null!;

    public async ValueTask InitializeAsync()
    {
        _initialArchive = await CreateArchiveAsync("type Query { value: String }");
        _updatedArchive = await CreateArchiveAsync("type Query { value: Int }");
    }

    public ValueTask DisposeAsync()
    {
        _directory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task RunAsync_Should_SubscribeBeforeQueryAndDeduplicateQueryEventVersion()
    {
        // arrange
        var stageClient = new ScriptedStageUpdateClient();
        var queried = Snapshot("configuration-2");
        stageClient.EnqueueSnapshot(queried);
        stageClient.EnqueueSubscription(
            new NitroStageChange(
                NitroStageChangeKind.FusionConfigurationPublished,
                FusionConfigurationId: "configuration-2"));
        var handler = new ArchiveSequenceHandler(_initialArchive, _updatedArchive);
        var coordinator = await CreateCoordinatorAsync(handler, stageClient, autoUpdate: true);
        var initialSeedPath = coordinator.GetSeed("gateway")!.FilePath;
        using var gate = new SemaphoreSlim(1, 1);
        using var stopping = new CancellationTokenSource();
        var recompositions = new List<NitroSeedAdoption>();
        var adoptions = new List<NitroSeedAdoption>();
        var monitor = CreateMonitor(
            coordinator,
            gate,
            adoption =>
            {
                recompositions.Add(adoption);
                return Task.FromResult(true);
            },
            staged => { },
            adoptions.Add,
            TimeProvider.System);

        // act
        monitor.Start(stopping.Token);
        await WaitUntilAsync(() => adoptions.Count == 1);
        await stopping.CancelAsync();
        await monitor.Completion.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        // assert
        $"""
        Operations: {string.Join(", ", stageClient.Operations.Take(3))}
        Downloads: {handler.RequestCount}
        Recompositions: {recompositions.Count}
        Adoptions: {adoptions.Count}
        Version: {adoptions[0].Candidate.VersionIdentity == queried.Identity}
        Retained seeds: {File.Exists(initialSeedPath)}|{File.Exists(coordinator.GetSeed("gateway")!.FilePath)}
        """.MatchInlineSnapshot(
            """
            Operations: subscribe, query, event:FusionConfigurationPublished
            Downloads: 2
            Recompositions: 1
            Adoptions: 1
            Version: True
            Retained seeds: False|True
            """);
    }

    [Fact]
    public async Task RunAsync_Should_SkipAdoption_WhenDownloadedHashIsUnchanged()
    {
        // arrange
        var stageClient = new ScriptedStageUpdateClient();
        stageClient.EnqueueSnapshot(Snapshot("configuration-1"));
        stageClient.EnqueueSubscription();
        var handler = new ArchiveSequenceHandler(_initialArchive, _initialArchive);
        var coordinator = await CreateCoordinatorAsync(handler, stageClient, autoUpdate: true);
        using var gate = new SemaphoreSlim(1, 1);
        using var stopping = new CancellationTokenSource();
        var recompositions = 0;
        var notifications = 0;
        var monitor = CreateMonitor(
            coordinator,
            gate,
            _ =>
            {
                recompositions++;
                return Task.FromResult(true);
            },
            _ => notifications++,
            _ => notifications++,
            TimeProvider.System);

        // act
        monitor.Start(stopping.Token);
        await WaitUntilAsync(() => handler.RequestCount == 2);
        await stopping.CancelAsync();
        await monitor.Completion.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("2|0|0", $"{handler.RequestCount}|{recompositions}|{notifications}");
    }

    [Fact]
    public async Task RunAsync_Should_ProcessOnlyNewestVersion_WhenCompositionGateIsHeld()
    {
        // arrange
        var stageClient = new ScriptedStageUpdateClient();
        var first = Snapshot("configuration-1");
        var secondChange = new NitroStageChange(
            NitroStageChangeKind.FusionConfigurationPublished,
            FusionConfigurationId: "configuration-2");
        var thirdChange = new NitroStageChange(
            NitroStageChangeKind.FusionConfigurationPublished,
            FusionConfigurationId: "configuration-3");
        var newest = first.Apply(secondChange).Apply(thirdChange);
        stageClient.EnqueueSnapshot(first);
        stageClient.EnqueueSubscription(secondChange, thirdChange);
        var handler = new ArchiveSequenceHandler(_initialArchive, _updatedArchive);
        var coordinator = await CreateCoordinatorAsync(handler, stageClient, autoUpdate: true);
        using var gate = new SemaphoreSlim(1, 1);
        await gate.WaitAsync(TestContext.Current.CancellationToken);
        using var stopping = new CancellationTokenSource();
        var adoptions = new List<NitroSeedAdoption>();
        var monitor = CreateMonitor(
            coordinator,
            gate,
            _ => Task.FromResult(true),
            _ => { },
            adoptions.Add,
            TimeProvider.System);

        // act
        monitor.Start(stopping.Token);
        await WaitUntilAsync(() => stageClient.Operations.Count >= 4);
        gate.Release();
        await WaitUntilAsync(() => adoptions.Count == 1);
        await stopping.CancelAsync();
        await monitor.Completion.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            $"2|1|{newest.Identity}",
            $"{handler.RequestCount}|{adoptions.Count}|{adoptions[0].Candidate.VersionIdentity}");
    }

    [Fact]
    public async Task RunAsync_Should_QueryAndAdopt_WhenSubscriptionCannotBeEstablished()
    {
        // arrange
        var stageClient = new ScriptedStageUpdateClient();
        stageClient.EnqueueSnapshot(Snapshot("configuration-2"));
        stageClient.EnqueueSubscriptionFailure(new HttpRequestException("SSE unavailable"));
        var handler = new ArchiveSequenceHandler(_initialArchive, _updatedArchive);
        var coordinator = await CreateCoordinatorAsync(handler, stageClient, autoUpdate: true);
        using var gate = new SemaphoreSlim(1, 1);
        using var stopping = new CancellationTokenSource();
        var adoptions = 0;
        var monitor = CreateMonitor(
            coordinator,
            gate,
            _ => Task.FromResult(true),
            _ => { },
            _ => adoptions++,
            TimeProvider.System);

        // act
        monitor.Start(stopping.Token);
        await WaitUntilAsync(() => adoptions == 1);
        await stopping.CancelAsync();
        await monitor.Completion.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("subscribe, query|2|1", $"{string.Join(", ", stageClient.Operations)}|{handler.RequestCount}|{adoptions}");
    }

    [Fact]
    public async Task SetAutoUpdateAsync_Should_AdoptStagedVersion_WhenEnabled()
    {
        // arrange
        var stageClient = new ScriptedStageUpdateClient();
        stageClient.EnqueueSnapshot(Snapshot("configuration-2"));
        stageClient.EnqueueBlockingSubscription();
        var handler = new ArchiveSequenceHandler(_initialArchive, _updatedArchive);
        var coordinator = await CreateCoordinatorAsync(handler, stageClient, autoUpdate: false);
        using var gate = new SemaphoreSlim(1, 1);
        using var stopping = new CancellationTokenSource();
        var staged = 0;
        var recompositions = 0;
        var adoptions = 0;
        var monitor = CreateMonitor(
            coordinator,
            gate,
            _ =>
            {
                recompositions++;
                return Task.FromResult(true);
            },
            _ => staged++,
            _ => adoptions++,
            TimeProvider.System);

        // act
        monitor.Start(stopping.Token);
        await WaitUntilAsync(() => staged == 1);
        var beforeEnable = coordinator.GetStagedCandidate("gateway") is not null;
        await monitor.SetAutoUpdateAsync(
            enabled: true,
            TestContext.Current.CancellationToken);
        await stopping.CancelAsync();
        await monitor.Completion.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        // assert
        $"""
        Staged before enable: {beforeEnable}
        Staged notifications: {staged}
        Recompositions: {recompositions}
        Adoptions: {adoptions}
        Auto-update: {coordinator.IsAutoUpdateEnabled("gateway")}
        """.MatchInlineSnapshot(
            """
            Staged before enable: True
            Staged notifications: 1
            Recompositions: 1
            Adoptions: 1
            Auto-update: True
            """);
    }

    [Fact]
    public async Task RunAsync_Should_GrowBackoffAndResetAfterSuccessfulConnection()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var stageClient = new ScriptedStageUpdateClient();
        stageClient.EnqueueSnapshot(null);
        stageClient.EnqueueSnapshot(null);
        stageClient.EnqueueSnapshot(null);
        stageClient.EnqueueSubscriptionFailure(new HttpRequestException("first"));
        stageClient.EnqueueSubscriptionFailure(new HttpRequestException("second"));
        stageClient.EnqueueSubscription();
        var handler = new ArchiveSequenceHandler(_initialArchive);
        var coordinator = await CreateCoordinatorAsync(handler, stageClient, autoUpdate: true);
        using var gate = new SemaphoreSlim(1, 1);
        using var stopping = new CancellationTokenSource();
        var delays = new ConcurrentQueue<TimeSpan>();
        var monitor = CreateMonitor(
            coordinator,
            gate,
            _ => Task.FromResult(true),
            _ => { },
            _ => { },
            timeProvider);
        monitor.RetryScheduled = delays.Enqueue;

        // act
        monitor.Start(stopping.Token);
        await WaitUntilAsync(() => delays.Count == 1);
        timeProvider.Advance(TimeSpan.FromSeconds(13));
        await WaitUntilAsync(() => delays.Count == 2);
        timeProvider.Advance(TimeSpan.FromSeconds(26));
        await WaitUntilAsync(() => delays.Count == 3);
        await stopping.CancelAsync();
        await monitor.Completion.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        // assert
        var observed = delays.ToArray();
        Assert.InRange(observed[0], TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12.5));
        Assert.InRange(observed[1], TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(25));
        Assert.InRange(observed[2], TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12.5));
    }

    [Fact]
    public async Task RunAsync_Should_RetryVersionAfterAdoptionFailsOnPreviousCycle()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var stageClient = new ScriptedStageUpdateClient();
        var version = Snapshot("configuration-2");
        stageClient.EnqueueSnapshot(version);
        stageClient.EnqueueSnapshot(version);
        stageClient.EnqueueSubscription();
        stageClient.EnqueueBlockingSubscription();
        var handler = new ArchiveSequenceHandler(
            _initialArchive,
            _updatedArchive,
            _updatedArchive);
        var coordinator = await CreateCoordinatorAsync(handler, stageClient, autoUpdate: true);
        var originalHash = coordinator.GetSeed("gateway")!.SchemaHash;
        using var gate = new SemaphoreSlim(1, 1);
        using var stopping = new CancellationTokenSource();
        var attempts = 0;
        var adoptions = 0;
        var monitor = CreateMonitor(
            coordinator,
            gate,
            _ => Task.FromResult(++attempts > 1),
            _ => { },
            _ => adoptions++,
            timeProvider);

        // act
        monitor.Start(stopping.Token);
        await WaitUntilAsync(() => attempts == 1);
        var rolledBack = coordinator.GetSeed("gateway")!.SchemaHash == originalHash;
        var filesAfterRollback = Directory.GetFiles(_directory.GetPath("run"), "*.far").Length;
        timeProvider.Advance(TimeSpan.FromSeconds(13));
        await WaitUntilAsync(() => attempts == 2);
        await stopping.CancelAsync();
        await monitor.Completion.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            "True|1|3|2|1",
            $"{rolledBack}|{filesAfterRollback}|{handler.RequestCount}|{attempts}|{adoptions}");
    }

    private async Task<NitroSeedCoordinator> CreateCoordinatorAsync(
        ArchiveSequenceHandler handler,
        INitroStageUpdateClient stageClient,
        bool autoUpdate)
    {
        var timeProvider = TimeProvider.System;
        var httpClient = new HttpClient(handler);
        var connectionResolver = new NitroConnectionResolver(
            new NitroSessionReader(_directory.GetPath("session.json"), TimeSpan.Zero),
            new TestNitroEnvironment(
                (NitroEnvironmentVariables.CloudUrl, "https://nitro.example.test"),
                (NitroEnvironmentVariables.ApiKey, "key")),
            NitroDefaults.ApiUrl,
            timeProvider,
            NitroDefaults.AccessTokenExpiryGrace);
        var coordinator = new NitroSeedCoordinator(
            Stage,
            connectionResolver,
            new NitroSeedProvider(
                new NitroFusionConfigurationDownloader(
                    httpClient,
                    new NitroDownloadRetryPolicy(1, 1, TimeSpan.Zero),
                    timeProvider),
                new NitroSeedCache(_directory.GetPath("cache"), timeProvider),
                new NitroApiLookupClient(
                    GraphQLHttpClient.Create(httpClient, disposeHttpClient: false))),
            NoopSchemaValidator.Instance,
            stageClient,
            NoopCompositionSettingsClient.Instance,
            _directory.GetPath("run"),
            autoUpdate);

        var acquisition = await coordinator.AcquireSeedAsync(
            "gateway",
            ApiId,
            new RecordingLogger<NitroSeedUpdateMonitorTests>(),
            TestContext.Current.CancellationToken);
        if (acquisition.Seed is null)
        {
            throw new InvalidOperationException(acquisition.FailureMessage);
        }

        return coordinator;
    }

    private static NitroSeedUpdateMonitor CreateMonitor(
        NitroSeedCoordinator coordinator,
        SemaphoreSlim gate,
        Func<NitroSeedAdoption, Task<bool>> recomposeAsync,
        Action<NitroSeedCandidate> notifyStaged,
        Action<NitroSeedAdoption> reportAdoption,
        TimeProvider timeProvider)
        => new(
            "gateway",
            ApiId,
            coordinator,
            gate,
            (adoption, _) => recomposeAsync(adoption),
            new RecordingLogger<NitroSeedUpdateMonitorTests>(),
            notifyStaged,
            reportAdoption,
            timeProvider,
            new RecordingLogger<NitroSeedUpdateMonitor>());

    private static NitroStageSnapshot Snapshot(string? configurationId)
        => new(
            configurationId,
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal));

    private static Task<byte[]> CreateArchiveAsync(string schema)
        => NitroTestArchive.CreateAsync(
            TestContext.Current.CancellationToken,
            new NitroTestSourceSchema(
                "products",
                schema,
                """
                {
                  "name": "products",
                  "transports": {
                    "http": {
                      "url": "https://products.example.test/graphql"
                    }
                  }
                }
                """));

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private sealed class ArchiveSequenceHandler(params byte[][] archives) : HttpMessageHandler
    {
        private readonly ConcurrentQueue<byte[]> _archives = new(archives);
        private byte[]? _lastArchive = archives.LastOrDefault();
        private int _requestCount;

        public int RequestCount => Volatile.Read(ref _requestCount);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requestCount);
            if (_archives.TryDequeue(out var archive))
            {
                _lastArchive = archive;
            }

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(_lastArchive ?? [])
                });
        }
    }

    private sealed class ScriptedStageUpdateClient : INitroStageUpdateClient
    {
        private readonly ConcurrentQueue<SubscriptionPlan> _subscriptions = new();
        private readonly ConcurrentQueue<NitroStageSnapshot?> _snapshots = new();

        public ConcurrentQueue<string> Operations { get; } = new();

        public void EnqueueSnapshot(NitroStageSnapshot? snapshot)
            => _snapshots.Enqueue(snapshot);

        public void EnqueueSubscription(params NitroStageChange[] changes)
            => _subscriptions.Enqueue(new SubscriptionPlan(changes, Block: false, Failure: null));

        public void EnqueueBlockingSubscription()
            => _subscriptions.Enqueue(new SubscriptionPlan([], Block: true, Failure: null));

        public void EnqueueSubscriptionFailure(Exception exception)
            => _subscriptions.Enqueue(new SubscriptionPlan([], Block: false, exception));

        public Task<NitroStageSubscription> SubscribeAsync(
            NitroConnection connection,
            string apiId,
            string stage,
            CancellationToken cancellationToken)
        {
            Operations.Enqueue("subscribe");
            if (!_subscriptions.TryDequeue(out var plan))
            {
                plan = new SubscriptionPlan([], Block: true, Failure: null);
            }

            return plan.Failure is null
                ? Task.FromResult<NitroStageSubscription>(
                    new ScriptedSubscription(plan, Operations))
                : Task.FromException<NitroStageSubscription>(plan.Failure);
        }

        public Task<NitroStageSnapshot?> GetLatestSnapshotAsync(
            NitroConnection connection,
            string apiId,
            string stage,
            CancellationToken cancellationToken)
        {
            Operations.Enqueue("query");
            _snapshots.TryDequeue(out var snapshot);
            return Task.FromResult(snapshot);
        }

        private sealed class ScriptedSubscription(
            SubscriptionPlan plan,
            ConcurrentQueue<string> operations)
            : NitroStageSubscription
        {
            public override async IAsyncEnumerable<NitroStageChange> ReadChangesAsync(
                [System.Runtime.CompilerServices.EnumeratorCancellation]
                CancellationToken cancellationToken)
            {
                foreach (var change in plan.Changes)
                {
                    operations.Enqueue("event:" + change.Kind);
                    yield return change;
                }

                if (plan.Block)
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
            }

            public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }

        private sealed record SubscriptionPlan(
            IReadOnlyList<NitroStageChange> Changes,
            bool Block,
            Exception? Failure);
    }

    private sealed class NoopSchemaValidator : INitroSchemaValidator
    {
        public static NoopSchemaValidator Instance { get; } = new();

        public Task<NitroSchemaValidationReport> ValidateAsync(
            NitroConnection connection,
            string apiId,
            string stage,
            byte[] schema,
            string schemaHash,
            CancellationToken cancellationToken)
            => Task.FromResult(
                NitroSchemaValidationReport.Passed(
                    schemaHash,
                    requestId: "noop",
                    DateTimeOffset.UtcNow));
    }

    private sealed class NoopCompositionSettingsClient : INitroCompositionSettingsClient
    {
        public static NoopCompositionSettingsClient Instance { get; } = new();

        public Task<CompositionSettings?> GetAsync(
            NitroConnection connection,
            string apiId,
            string stage,
            CancellationToken cancellationToken)
            => Task.FromResult<CompositionSettings?>(null);
    }
}
