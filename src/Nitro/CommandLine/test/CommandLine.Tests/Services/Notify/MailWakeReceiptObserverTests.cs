using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="MailWakeReceiptObserver"/> against a real workspace
/// database: a receipt observed before any batch has claimed its generation
/// is pending, observing after <see cref="ActorWakeDispatcher"/> delivers
/// reports delivered zero, a terminal failure recorded after a prior
/// acceptance on the same target still reports failed, an access-denied
/// offer leaves a pending row with its offered generation, and two
/// consecutive calls for the same receipt see whatever changed between them
/// (no caching across calls).
/// </summary>
public sealed class MailWakeReceiptObserverTests : IDisposable
{
    private const string Actor = "codex-worker";
    private const string InstanceId = "host-1";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentDatabase _database;
    private readonly AgentRegistry _agentRegistry;
    private readonly AgentSessionRegistry _sessions;
    private readonly MailStore _mail;
    private readonly MailWakeBatchStore _batches;
    private readonly SessionGateCoordinator _gateCoordinator;
    private readonly FixedInstanceIdProvider _instanceIdProvider = new(InstanceId);
    private readonly FixedGlobalConfigDirectoryProvider _globalConfigDirectoryProvider;
    private readonly MailWakeReceiptObserver _observer;

    public MailWakeReceiptObserverTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-mail-wake-receipt-observer-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        _database = new AgentDatabase();
        _agentRegistry = new AgentRegistry(_fileSystem, _timeProvider, _database);
        _sessions = new AgentSessionRegistry(
            _fileSystem,
            _timeProvider,
            _database,
            _agentRegistry,
            _instanceIdProvider,
            new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName),
            new ProcessInfoProvider(),
            new FixedAncestorSessionResolver(null));
        _globalConfigDirectoryProvider = new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName);
        _mail = new MailStore(
            _fileSystem, _timeProvider, _database, _agentRegistry, _instanceIdProvider, _globalConfigDirectoryProvider);
        _batches = new MailWakeBatchStore(_fileSystem, _database);
        _gateCoordinator = new SessionGateCoordinator(
            new SessionPingGateStore(_fileSystem, _database), new PingLeaseStore(_fileSystem, _database));
        _observer = new MailWakeReceiptObserver(
            _fileSystem, _database, _instanceIdProvider, _globalConfigDirectoryProvider);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task ObserveAsync_Should_ReturnPendingNonzero_When_ObservedBeforeAnyClaim()
    {
        // arrange: mail enqueued, but no dispatcher has ever run for this
        // actor, so no batch has claimed the receipt's generation yet.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var message = await SendEnqueuedMailAsync(cancellationToken);
        var receipt = Assert.Single(message.WakeReceipts);

        // act
        var observation = await _observer.ObserveAsync(receipt, Deadline(), cancellationToken);

        // assert
        Assert.Equal(MailWakeTargetStatus.Pending, observation.Status);
        Assert.False(observation.IsSuccessful);
        Assert.Empty(observation.Targets);
    }

    [Fact]
    public async Task ObserveAsync_Should_ReturnDeliveredZero_When_ActorWakeDispatcherDelivers()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        var message = await SendEnqueuedMailAsync(cancellationToken);
        var receipt = Assert.Single(message.WakeReceipts);
        var dispatcher = CreateDispatcher(new FakePingSessionExecutor());
        await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

        // act
        var observation = await _observer.ObserveAsync(receipt, Deadline(), cancellationToken);

        // assert: delivered zero, the exact frozen target, and no duplicate
        // message row was written by a send preceding this observation.
        Assert.Equal(MailWakeTargetStatus.Delivered, observation.Status);
        Assert.True(observation.IsSuccessful);
        var target = Assert.Single(observation.Targets);
        Assert.Equal(generation, target.Target);
        var sent = await _mail.QuerySentAsync("pascal", null, cancellationToken);
        Assert.Single(sent);
    }

    [Fact]
    public async Task ObserveAsync_Should_ReportFailed_When_ATerminalFailureRacesAPriorAcceptance()
    {
        // arrange: an earlier call already recorded this target Delivered
        // with an accepted generation, then a later call for the same
        // owner/attempt recorded a terminal Failed for the same target -
        // the observer must report the terminal outcome, never the
        // historical acceptance. The Notify-scope form of the store-level
        // coverage in MailWakeBatchStoreTests.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        var message = await SendEnqueuedMailAsync(cancellationToken);
        var receipt = Assert.Single(message.WakeReceipts);
        var now = _timeProvider.GetUtcNow();
        var claim = await _batches.TryClaimAsync(
            InstanceId, Actor, "owner-1", "attempt-1", [generation], now,
            WakeDispatchPolicy.BatchLeaseDuration, cancellationToken);
        Assert.NotNull(claim);
        await _batches.TryRecordTargetOutcomeAsync(
            claim.BatchId, generation, "owner-1", "attempt-1", MailWakeTargetStatus.Delivered,
            offeredGeneration: null, acceptedGeneration: receipt.Generation, lastError: null, now, cancellationToken);
        await _batches.TryRecordTargetOutcomeAsync(
            claim.BatchId, generation, "owner-1", "attempt-1", MailWakeTargetStatus.Failed,
            offeredGeneration: null, acceptedGeneration: null, lastError: "endpoint-gone",
            now + TimeSpan.FromSeconds(1), cancellationToken);

        // act
        var observation = await _observer.ObserveAsync(receipt, Deadline(), cancellationToken);

        // assert
        Assert.Equal(MailWakeTargetStatus.Failed, observation.Status);
        Assert.False(observation.IsSuccessful);
        var target = Assert.Single(observation.Targets);
        Assert.Equal(MailWakeTargetStatus.Failed, target.Status);
    }

    [Fact]
    public async Task ObserveAsync_Should_ReturnPendingWithOfferedGeneration_When_ClaudeAccessIsDenied()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.ClaudePeer, "peer-a", cancellationToken, sessionId: "claude-session");
        var message = await SendEnqueuedMailAsync(cancellationToken);
        var receipt = Assert.Single(message.WakeReceipts);
        var executor = new FakePingSessionExecutor { NextReason = PingAttemptReason.AccessDenied };
        var dispatcher = CreateDispatcher(executor);
        await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

        // act
        var observation = await _observer.ObserveAsync(receipt, Deadline(), cancellationToken);

        // assert
        Assert.Equal(MailWakeTargetStatus.Pending, observation.Status);
        Assert.False(observation.IsSuccessful);
        var target = Assert.Single(observation.Targets);
        Assert.Equal(generation, target.Target);
        Assert.NotNull(target.OfferedGeneration);
    }

    [Fact]
    public async Task ObserveAsync_Should_SeeAChangeMadeBetweenTwoCalls_When_ObservedTwice()
    {
        // arrange: no caching - the first call observes before any batch has
        // claimed the generation, the second observes after
        // ActorWakeDispatcher delivered it.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        var message = await SendEnqueuedMailAsync(cancellationToken);
        var receipt = Assert.Single(message.WakeReceipts);

        // act
        var before = await _observer.ObserveAsync(receipt, Deadline(), cancellationToken);
        var dispatcher = CreateDispatcher(new FakePingSessionExecutor());
        await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);
        var after = await _observer.ObserveAsync(receipt, Deadline(), cancellationToken);

        // assert
        Assert.Equal(MailWakeTargetStatus.Pending, before.Status);
        Assert.Equal(MailWakeTargetStatus.Delivered, after.Status);
    }

    private DateTimeOffset Deadline() => _timeProvider.GetUtcNow() + WakeDispatchPolicy.BatchDeadline;

    private ActorWakeDispatcher CreateDispatcher(FakePingSessionExecutor executor)
        => new(
            _batches,
            _sessions,
            _gateCoordinator,
            executor,
            _mail,
            _instanceIdProvider,
            _globalConfigDirectoryProvider,
            _timeProvider);

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }

    private async Task<AgentSessionGeneration> SeedLiveSessionAsync(
        string endpointKind,
        string endpointAddr,
        CancellationToken cancellationToken,
        string sessionId = "session-1")
    {
        // A genuinely alive pid/proc_start: dispatch resolves live sessions
        // through FindLiveClaimedByAgentNameAsync, which reaps dead
        // current-instance rows first, so a fake sentinel pid would be
        // reaped out from under the test before it ever reached dispatch.
        var pid = Environment.ProcessId;
        var procStart = new ProcessInfoProvider().GetStartTicks(pid)!;
        var harness = endpointKind == AgentSessionEndpointKind.ClaudePeer
            ? AgentSessionHarness.ClaudeCode
            : AgentSessionHarness.Codex;
        var generation = new AgentSessionGeneration(harness, sessionId, InstanceId, pid, procStart);

        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", endpointKind, endpointAddr,
            envActor: Actor, cancellationToken);

        return generation;
    }

    private async Task<MailMessage> SendEnqueuedMailAsync(CancellationToken cancellationToken)
        => await _mail.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = "pascal",
                Subject = "status",
                Body = "check",
                To = [Actor],
                WakePolicy = MailWakePolicy.Enqueue
            },
            cancellationToken);
}
