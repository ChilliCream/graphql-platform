using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="Notifier"/> against a real workspace database: the
/// claude-peer/none/codex-thread branch, the cooldown/lease decision before
/// any spawn, spawn-failure recording, and never throwing - the required
/// notifier tests for capacity-dropped, supported-vs-none distinctness,
/// and clean behavior under every spawn-failure mode.
/// </summary>
public sealed class NotifierTests : IDisposable
{
    private const string Actor = "codex-worker";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentDatabase _database;
    private readonly AgentRegistry _agentRegistry;
    private readonly AgentSessionRegistry _sessions;
    private readonly PingLeaseStore _leases;
    private readonly FakePingWorkerLauncher _launcher;
    private readonly Notifier _notifier;

    public NotifierTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-notifier-tests");
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
            new FixedInstanceIdProvider("host-1"),
            new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName),
            new ProcessInfoProvider(),
            new FixedAncestorSessionResolver(null));
        _leases = new PingLeaseStore(_fileSystem, _database);
        _launcher = new FakePingWorkerLauncher();
        _notifier = new Notifier(
            _sessions, _leases, _launcher, new FixedLaunchDescriptorResolver(), _timeProvider);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task NotifyAsync_Should_SpawnTheWorker_When_EndpointIsClaudePeer()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var generation = await SeedClaimedSessionAsync(
            AgentSessionEndpointKind.ClaudePeer, "peer-a", cancellationToken);

        // act
        await _notifier.NotifyAsync([Actor], cancellationToken);

        // assert
        var call = Assert.Single(_launcher.Calls);
        Assert.Contains("--endpoint-kind", call.WorkerArgs);
        Assert.Contains(AgentSessionEndpointKind.ClaudePeer, call.WorkerArgs);
        Assert.Contains("--pid", call.WorkerArgs);

        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.NotNull(row!.LastPingAttempt);
        Assert.Null(row.LastPingResult);
    }

    [Fact]
    public async Task NotifyAsync_Should_RecordUnsupported_And_NeverSpawn_When_EndpointHasNoTransport()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var generation = await SeedClaimedSessionAsync(
            AgentSessionEndpointKind.CopilotExtension, "copilot-a", cancellationToken);

        // act
        await _notifier.NotifyAsync([Actor], cancellationToken);

        // assert
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.Equal(AgentPingResult.Unsupported, row!.LastPingResult);
        Assert.Empty(_launcher.Calls);
    }

    [Fact]
    public async Task NotifyAsync_Should_DoNothing_When_EndpointIsNone()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var generation = await SeedClaimedSessionAsync(AgentSessionEndpointKind.None, "", cancellationToken);

        // act
        await _notifier.NotifyAsync([Actor], cancellationToken);

        // assert: no endpoint to attempt at all - never recorded as a
        // distinct ping outcome.
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.Null(row!.LastPingResult);
        Assert.Empty(_launcher.Calls);
    }

    [Fact]
    public async Task NotifyAsync_Should_SpawnTheWorker_When_EndpointIsCodexThread()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var generation = await SeedClaimedSessionAsync(
            AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);

        // act
        await _notifier.NotifyAsync([Actor], cancellationToken);

        // assert: cooldown claimed and a lease acquired before spawning, so
        // the row carries a pending attempt id; the worker (not this
        // process) writes the eventual result.
        var call = Assert.Single(_launcher.Calls);
        Assert.Contains("ping-worker", call.WorkerArgs);
        Assert.Contains(generation.SessionId, call.WorkerArgs);
        Assert.Contains("thread-1", call.WorkerArgs);
        Assert.Contains(Actor, call.WorkerArgs);
        Assert.Contains(AgentSessionEndpointKind.CodexThread, call.WorkerArgs);
        Assert.Contains("--deadline", call.WorkerArgs);

        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.NotNull(row!.LastPingAttempt);
        Assert.Null(row.LastPingResult);
    }

    [Fact]
    public async Task NotifyAsync_Should_RecordSpawnFailed_And_ReleaseTheLease_When_LaunchFails()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var generation = await SeedClaimedSessionAsync(
            AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        _launcher.NextResult = false;

        // act
        await _notifier.NotifyAsync([Actor], cancellationToken);

        // assert
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.Equal(AgentPingResult.SpawnFailed, row!.LastPingResult);

        // the lease was released, not leaked: all four slots are free again.
        var acquired = await Task.WhenAll(Enumerable.Range(1, 4).Select(i =>
            _leases.TryAcquireAsync($"probe-{i}", _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken)));
        Assert.All(acquired, slot => Assert.NotNull(slot));
    }

    [Fact]
    public async Task NotifyAsync_Should_RecordCapacityDropped_When_AllLeaseSlotsAreHeld()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var generation = await SeedClaimedSessionAsync(
            AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);

        for (var i = 1; i <= 4; i++)
        {
            await _leases.TryAcquireAsync(
                $"holder-{i}", _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        }

        // act
        await _notifier.NotifyAsync([Actor], cancellationToken);

        // assert: recorded, and no spawn was ever attempted.
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.Equal(AgentPingResult.CapacityDropped, row!.LastPingResult);
        Assert.Empty(_launcher.Calls);
    }

    [Fact]
    public async Task NotifyAsync_Should_Coalesce_When_CalledTwiceWithinTheCooldown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedClaimedSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);

        // act
        await _notifier.NotifyAsync([Actor], cancellationToken);
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        await _notifier.NotifyAsync([Actor], cancellationToken);

        // assert: the second call landed inside the 60s cooldown, so it
        // never spawned a second worker.
        Assert.Single(_launcher.Calls);
    }

    [Fact]
    public async Task NotifyAsync_Should_CoalesceUnsupported_When_CalledTwiceWithinTheCooldown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var generation = await SeedClaimedSessionAsync(
            AgentSessionEndpointKind.CopilotExtension, "copilot-a", cancellationToken);

        // act
        await _notifier.NotifyAsync([Actor], cancellationToken);
        var firstAttempt = (await _sessions.FindByGenerationAsync(generation, cancellationToken))!.LastPingAttempt;
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        await _notifier.NotifyAsync([Actor], cancellationToken);

        // assert: the second call landed inside the 60s cooldown, so it
        // never claimed a fresh attempt id.
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.Equal(firstAttempt, row!.LastPingAttempt);
    }

    [Fact]
    public async Task NotifyAsync_Should_NeverThrow_When_NoWorkspaceExists()
    {
        // arrange: InitializeWorkspaceAsync was never called - resolving the
        // workspace throws inside FindLiveClaimedByAgentNameAsync.
        var cancellationToken = TestContext.Current.CancellationToken;

        // act & assert: mail success output and exit code can never be
        // altered by a notification failure.
        await _notifier.NotifyAsync([Actor], cancellationToken);
    }

    private async Task<AgentSessionGeneration> SeedClaimedSessionAsync(
        string endpointKind, string endpointAddr, CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }

        // A genuinely alive pid/proc_start: NotifyAsync resolves live
        // sessions through FindLiveClaimedByAgentNameAsync, which reaps
        // dead current-instance rows first, so a fake sentinel pid would be
        // reaped out from under the test before it ever reached the
        // endpoint-kind branch.
        var pid = Environment.ProcessId;
        var procStart = new ProcessInfoProvider().GetStartTime(pid)!.Value;

        var harness = endpointKind == AgentSessionEndpointKind.ClaudePeer
            ? AgentSessionHarness.ClaudeCode
            : AgentSessionHarness.Codex;
        var generation = new AgentSessionGeneration(harness, "session-1", "host-1", pid, procStart);

        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", endpointKind, endpointAddr,
            envActor: Actor, cancellationToken);

        return generation;
    }
}

internal sealed record FakePingWorkerLaunchCall(LaunchDescriptor Descriptor, IReadOnlyList<string> WorkerArgs);

internal sealed class FakePingWorkerLauncher : IPingWorkerLauncher
{
    public List<FakePingWorkerLaunchCall> Calls { get; } = [];

    public bool NextResult { get; set; } = true;

    public bool TryLaunch(LaunchDescriptor descriptor, IReadOnlyList<string> workerArgs)
    {
        Calls.Add(new FakePingWorkerLaunchCall(descriptor, workerArgs));
        return NextResult;
    }
}

internal sealed class FixedLaunchDescriptorResolver : ILaunchDescriptorResolver
{
    public LaunchDescriptor Resolve() => new("/usr/bin/nitro", []);
}
