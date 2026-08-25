using System.Diagnostics;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="AgentSessionRegistry"/>'s lifecycle: SessionStart
/// binding rules and same/different-generation handling, the v5
/// role/harness_version/process_scope columns defaulting on creation,
/// surviving a same-generation duplicate SessionStart, and resetting on a
/// different-generation rebind, the claim state machine's five transitions
/// plus force-rebind, conditional SessionEnd, reaping (current-instance dead
/// rows only, remote rows untouched), the one-row-per-session participant
/// read model joining durable agent identity when bound, and TOCTOU safety
/// when a generation changes between a reader's observation and its
/// mutation.
/// </summary>
public sealed class AgentSessionRegistryTests : IDisposable
{
    private const string Harness = AgentSessionHarness.ClaudeCode;
    private const string CurrentHost = "host-current";
    private const string RemoteHost = "host-remote";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentDatabase _database;
    private readonly AgentRegistry _agentRegistry;
    private readonly AgentSessionRegistry _sessions;

    public AgentSessionRegistryTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-agent-session-registry-tests");
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
            new FixedInstanceIdProvider(CurrentHost),
            new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName),
            new ProcessInfoProvider(),
            new FixedAncestorSessionResolver(null));
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    // ---------- StartAsync ----------

    [Fact]
    public async Task StartAsync_Should_CreateUnclaimedRow_When_NoEnvActorGiven()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");

        // act
        var record = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // assert
        Assert.Null(record.AgentName);
        Assert.Equal(AgentSessionBindingKind.None, record.BindingKind);
    }

    [Fact]
    public async Task StartAsync_Should_CreateEnvBoundRow_When_EnvActorGiven()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");

        // act
        var record = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "pascal", cancellationToken);

        // assert
        Assert.Equal("pascal", record.AgentName);
        Assert.Equal(AgentSessionBindingKind.Env, record.BindingKind);
    }

    [Fact]
    public async Task StartAsync_Should_DefaultRoleAndHarnessVersion_And_CaptureProcessScope_When_RowIsCreated()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");

        // act
        var record = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // assert: role and harness_version are not captured yet (a later
        // bead's job), but process_scope is captured at StartAsync time -
        // this test process's own scope, since it is also the writer here.
        Assert.Equal("", record.Role);
        Assert.Equal("", record.HarnessVersion);
        Assert.Equal(new ProcessInfoProvider().GetProcessScope(), record.ProcessScope);
    }

    [Fact]
    public async Task FindByGenerationAsync_Should_RoundTripRoleHarnessVersionAndProcessScope()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await SetSessionMetadataAsync(generation, "frontend", "1.2.3-beta", "pidns:4242", cancellationToken);

        // act
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);

        // assert
        Assert.Equal("frontend", row!.Role);
        Assert.Equal("1.2.3-beta", row.HarnessVersion);
        Assert.Equal("pidns:4242", row.ProcessScope);
    }

    [Fact]
    public async Task StartAsync_Should_PreserveHarnessMetadataAndRole_When_SameGenerationDuplicateSessionStart()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await SetSessionMetadataAsync(generation, "backend", "2.1.0", "scope-a", cancellationToken);

        // act: a duplicate SessionStart for the exact same generation must
        // not reset metadata captured after the row was first created.
        var record = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // assert
        Assert.Equal("backend", record.Role);
        Assert.Equal("2.1.0", record.HarnessVersion);
        Assert.Equal("scope-a", record.ProcessScope);
    }

    [Fact]
    public async Task StartAsync_Should_ResetHarnessMetadataAndRole_When_GenerationChanges()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var firstGeneration = AliveGeneration("session-1");
        await _sessions.StartAsync(
            firstGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await SetSessionMetadataAsync(firstGeneration, "backend", "2.1.0", "scope-a", cancellationToken);

        var restartedGeneration = firstGeneration with { Pid = DeadPid };

        // act: a new process replaced the one the row remembered - metadata
        // observed under the OLD generation must not leak into the new one.
        var record = await _sessions.StartAsync(
            restartedGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // assert: role and harness_version reset to blank (not re-captured
        // yet); process_scope is re-captured fresh rather than left at the
        // old generation's recorded value.
        Assert.Equal("", record.Role);
        Assert.Equal("", record.HarnessVersion);
        Assert.Equal(new ProcessInfoProvider().GetProcessScope(), record.ProcessScope);
    }

    [Fact]
    public async Task StartAsync_Should_PreserveState_When_SameGenerationDuplicateSessionStart()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "pascal", cancellationToken);
        var claimed = await _sessions.ClaimAsync(generation, "pascal", forceRebind: false, cancellationToken);
        Assert.Equal(AgentSessionBindingKind.Explicit, claimed.Session.BindingKind);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act: a duplicate SessionStart for the exact same generation, this
        // time carrying a DIFFERENT env actor - it must not overwrite the
        // explicit claim already in place.
        var record = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "someone-else", cancellationToken);

        // assert
        Assert.Equal("pascal", record.AgentName);
        Assert.Equal(AgentSessionBindingKind.Explicit, record.BindingKind);
        Assert.True(record.LastBeatAt > claimed.Session.LastBeatAt);
    }

    [Fact]
    public async Task StartAsync_Should_ResetLedgerAndRebind_When_GenerationChanges()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var firstGeneration = AliveGeneration("session-1");
        await _sessions.StartAsync(
            firstGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "pascal", cancellationToken);
        await _sessions.ClaimAsync(firstGeneration, "pascal", forceRebind: false, cancellationToken);
        await InsertDeliveryAsync(firstGeneration, "msg-1", cancellationToken);

        var restartedGeneration = firstGeneration with { Pid = DeadPid };

        // act: a new process replaced the one the row remembered, under the
        // SAME (harness, session_id).
        var record = await _sessions.StartAsync(
            restartedGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "someone-else", cancellationToken);

        // assert
        Assert.Equal("someone-else", record.AgentName);
        Assert.Equal(AgentSessionBindingKind.Env, record.BindingKind);
        Assert.Equal(0, record.BlockBudgetUsed);
        Assert.Equal(0, await CountDeliveriesAsync(firstGeneration, cancellationToken));
    }

    [Fact]
    public async Task StartAsync_Should_AdoptTheProvisionalRow_When_ACanonicalSessionIdArrivesForTheSameProcessGeneration()
    {
        // arrange: a provisional row (no authoritative session id was
        // available yet) already claimed and carrying ledger/budget state
        // for this exact process generation.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var pid = CurrentAlivePid();
        var procStart = new ProcessInfoProvider().GetStartTicks(pid)!;
        var provisionalSessionId = AgentSessionProvisionalSessionId.Derive(Harness, CurrentHost, pid, procStart);
        var provisionalGeneration = new AgentSessionGeneration(Harness, provisionalSessionId, CurrentHost, pid, procStart);
        await _sessions.StartAsync(
            provisionalGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(provisionalGeneration, "pascal", forceRebind: false, cancellationToken);
        await InsertDeliveryAsync(provisionalGeneration, "msg-1", cancellationToken);
        await _sessions.IncrementBlockBudgetAsync(provisionalGeneration, cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act: the canonical session id arrives for the exact same process
        // generation.
        var canonicalGeneration = provisionalGeneration with { SessionId = "canonical-session-1" };
        var record = await _sessions.StartAsync(
            canonicalGeneration, "/other-work", "/other-work/.nitro/agents", AgentSessionEndpointKind.CodexThread,
            "canonical-session-1", envActor: null, cancellationToken);

        // assert: adopted under the canonical key - binding, role, delivery
        // ledger, and block budget carried over untouched from the
        // provisional row, only the endpoint, location, and heartbeat
        // refreshed, and no second row left behind.
        Assert.Equal("canonical-session-1", record.SessionId);
        Assert.Equal("pascal", record.AgentName);
        Assert.Equal(AgentSessionBindingKind.Explicit, record.BindingKind);
        Assert.Equal("", record.Role);
        Assert.Equal(1, record.BlockBudgetUsed);
        Assert.Equal(AgentSessionEndpointKind.CodexThread, record.EndpointKind);
        Assert.Equal("canonical-session-1", record.EndpointAddr);
        Assert.Equal("/other-work", record.Cwd);
        Assert.Equal("/other-work/.nitro/agents", record.WorkspacePath);
        Assert.Equal(_timeProvider.GetUtcNow(), record.LastBeatAt);
        // The delivery ledger's foreign key follows the row's new key (the
        // same re-key EndAsync's cascade would apply if the row were
        // deleted instead), so its row content survives under the
        // canonical session id rather than the provisional one.
        Assert.Equal(1, await CountDeliveriesAsync(canonicalGeneration, cancellationToken));
        Assert.Equal(0, await CountDeliveriesAsync(provisionalGeneration, cancellationToken));
        Assert.Equal(0, await CountSessionRowsAsync(provisionalGeneration, cancellationToken));
        Assert.Equal(1, await CountSessionRowsAsync(canonicalGeneration, cancellationToken));
        Assert.Equal(1, await CountAllSessionRowsAsync(_workspaceDirectory, cancellationToken));
    }

    [Fact]
    public async Task StartAsync_Should_AdoptTheProvisionalRow_When_AStaleRowAlreadyOccupiesTheCanonicalSessionId()
    {
        // arrange: a stale row already sits under the canonical session id,
        // left behind at an older process generation (for example a prior
        // process that never received a SessionEnd), and a provisional row
        // now exists for the live process generation.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var pid = CurrentAlivePid();
        var procStart = new ProcessInfoProvider().GetStartTicks(pid)!;
        var staleGeneration = new AgentSessionGeneration(Harness, "canonical-1", CurrentHost, pid, "1");
        await _sessions.StartAsync(
            staleGeneration, "/stale-work", "/stale-work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "stale-actor", cancellationToken);

        var provisionalSessionId = AgentSessionProvisionalSessionId.Derive(Harness, CurrentHost, pid, procStart);
        var provisionalGeneration = new AgentSessionGeneration(Harness, provisionalSessionId, CurrentHost, pid, procStart);
        await _sessions.StartAsync(
            provisionalGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(provisionalGeneration, "pascal", forceRebind: false, cancellationToken);

        // act: the canonical session id arrives for the live process
        // generation, while the stale row still occupies that same id.
        var canonicalGeneration = provisionalGeneration with { SessionId = "canonical-1" };
        var record = await _sessions.StartAsync(
            canonicalGeneration, "/other-work", "/other-work/.nitro/agents", AgentSessionEndpointKind.CodexThread,
            "canonical-1", envActor: null, cancellationToken);

        // assert: the adopted row's binding comes from the provisional row,
        // not the stale row it replaced, and the stale row leaves no second
        // participant behind.
        Assert.Equal("canonical-1", record.SessionId);
        Assert.Equal("pascal", record.AgentName);
        Assert.Equal(AgentSessionBindingKind.Explicit, record.BindingKind);
        Assert.Equal(1, await CountSessionRowsAsync(canonicalGeneration, cancellationToken));
        Assert.Equal(0, await CountSessionRowsAsync(provisionalGeneration, cancellationToken));
        Assert.Equal(1, await CountAllSessionRowsAsync(_workspaceDirectory, cancellationToken));
    }

    [Fact]
    public async Task StartAsync_Should_NotAdopt_When_TheExistingRowsSessionIdIsNotProvisional()
    {
        // arrange: an existing row shares this exact process generation but
        // was never provisional (its own SessionStart already carried a
        // canonical session id) - a duplicate SessionStart for a DIFFERENT
        // session id sharing the same process generation must never adopt
        // someone else's canonical row.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "pascal", cancellationToken);

        // act
        var otherGeneration = generation with { SessionId = "session-2" };
        await _sessions.StartAsync(
            otherGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "codex", cancellationToken);

        // assert: two independent rows, neither adopted into the other.
        Assert.Equal(1, await CountSessionRowsAsync(generation, cancellationToken));
        Assert.Equal(1, await CountSessionRowsAsync(otherGeneration, cancellationToken));
    }

    // ---------- ClaimAsync state machine ----------

    [Fact]
    public async Task ClaimAsync_Should_Bind_When_NoneToExplicit()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var result = await _sessions.ClaimAsync(generation, "pascal", forceRebind: false, cancellationToken);

        // assert
        Assert.True(result.Changed);
        Assert.Equal(AgentSessionBindingKind.None, result.PreviousBindingKind);
        Assert.Null(result.PreviousAgentName);
        Assert.Equal(AgentSessionBindingKind.Explicit, result.Session.BindingKind);
        Assert.Equal("pascal", result.Session.AgentName);
    }

    [Fact]
    public async Task ClaimAsync_Should_PromoteProvenanceWithoutResettingLedger_When_EnvToExplicitSameActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "pascal", cancellationToken);
        await InsertDeliveryAsync(generation, "msg-1", cancellationToken);

        // act
        var result = await _sessions.ClaimAsync(generation, "pascal", forceRebind: false, cancellationToken);

        // assert
        Assert.True(result.Changed);
        Assert.Equal(AgentSessionBindingKind.Env, result.PreviousBindingKind);
        Assert.Equal("pascal", result.PreviousAgentName);
        Assert.Equal(AgentSessionBindingKind.Explicit, result.Session.BindingKind);
        Assert.Equal(1, await CountDeliveriesAsync(generation, cancellationToken));
    }

    [Fact]
    public async Task ClaimAsync_Should_RebindAndResetLedger_When_EnvToExplicitDifferentActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "pascal", cancellationToken);
        await InsertDeliveryAsync(generation, "msg-1", cancellationToken);

        // act: the explicit act wins even though it targets a different
        // actor than the env binding, no --force-rebind needed here because
        // env provenance is not a protected explicit claim.
        var result = await _sessions.ClaimAsync(generation, "codex", forceRebind: false, cancellationToken);

        // assert
        Assert.True(result.Changed);
        Assert.Equal(AgentSessionBindingKind.Explicit, result.Session.BindingKind);
        Assert.Equal("codex", result.Session.AgentName);
        Assert.Equal(0, await CountDeliveriesAsync(generation, cancellationToken));
    }

    [Fact]
    public async Task ClaimAsync_Should_NoOp_When_ExplicitToExplicitSameActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(generation, "pascal", forceRebind: false, cancellationToken);
        await InsertDeliveryAsync(generation, "msg-1", cancellationToken);

        // act
        var result = await _sessions.ClaimAsync(generation, "pascal", forceRebind: false, cancellationToken);

        // assert
        Assert.False(result.Changed);
        Assert.Equal(1, await CountDeliveriesAsync(generation, cancellationToken));
    }

    [Fact]
    public async Task ClaimAsync_Should_Throw_When_ExplicitToExplicitDifferentActorWithoutForceRebind()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(generation, "pascal", forceRebind: false, cancellationToken);

        // act & assert
        var exception = await Assert.ThrowsAsync<ExitException>(
            () => _sessions.ClaimAsync(generation, "codex", forceRebind: false, cancellationToken));
        Assert.Contains("--force-rebind", exception.Message);
    }

    [Fact]
    public async Task ClaimAsync_Should_RebindAndResetLedger_When_ForceRebindOverridesExistingExplicitClaim()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(generation, "pascal", forceRebind: false, cancellationToken);
        await InsertDeliveryAsync(generation, "msg-1", cancellationToken);

        // act
        var result = await _sessions.ClaimAsync(generation, "codex", forceRebind: true, cancellationToken);

        // assert
        Assert.True(result.Changed);
        Assert.Equal("codex", result.Session.AgentName);
        Assert.Equal(0, await CountDeliveriesAsync(generation, cancellationToken));
    }

    [Fact]
    public async Task ClaimAsync_Should_Throw_When_GenerationDoesNotMatchAnyRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        var staleGeneration = generation with { Pid = generation.Pid + 999 };

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _sessions.ClaimAsync(staleGeneration, "pascal", forceRebind: false, cancellationToken));
    }

    // ---------- RegisterAsync ----------

    [Fact]
    public async Task RegisterAsync_Should_UpsertIdentityAndBindTheUnboundSession_When_BlankToRolePromotion()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var result = await _sessions.RegisterAsync(
            generation, "pascal", "orchestrator", "claude-code", forceRebind: false, cancellationToken);

        // assert
        Assert.True(result.Changed);
        Assert.Equal("pascal", result.Agent.Name);
        Assert.Equal("orchestrator", result.Agent.Role);
        Assert.Equal("claude-code", result.Agent.Client);
        Assert.Equal("pascal", result.Session.AgentName);
        Assert.Equal(AgentSessionBindingKind.Explicit, result.Session.BindingKind);
        Assert.Equal("orchestrator", result.Session.Role);
    }

    [Fact]
    public async Task RegisterAsync_Should_NormalizeRole_When_Written()
    {
        // arrange: the first writer of agent_sessions.role - proves
        // AgentRole.Normalize applies on the write path, not just reads.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var result = await _sessions.RegisterAsync(
            generation, "pascal", "  Backend  ", "", forceRebind: false, cancellationToken);

        // assert
        Assert.Equal("backend", result.Agent.Role);
        Assert.Equal("backend", result.Session.Role);
    }

    [Fact]
    public async Task RegisterAsync_Should_BeIdempotent_When_SameActorAndRoleAreRepeated()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        var first = await _sessions.RegisterAsync(
            generation, "pascal", "orchestrator", "", forceRebind: false, cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act: repeating the same actor and role is a no-op success that
        // only refreshes last-heard.
        var result = await _sessions.RegisterAsync(
            generation, "pascal", "orchestrator", "", forceRebind: false, cancellationToken);

        // assert
        Assert.False(result.Changed);
        Assert.Equal("orchestrator", result.Session.Role);
        Assert.True(result.Session.LastBeatAt > first.Session.LastBeatAt);
    }

    [Fact]
    public async Task RegisterAsync_Should_UpdateRole_When_RoleChanges()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.RegisterAsync(
            generation, "pascal", "orchestrator", "", forceRebind: false, cancellationToken);

        // act
        var result = await _sessions.RegisterAsync(
            generation, "pascal", "planner", "", forceRebind: false, cancellationToken);

        // assert
        Assert.True(result.Changed);
        Assert.Equal("planner", result.Agent.Role);
        Assert.Equal("planner", result.Session.Role);
    }

    [Fact]
    public async Task RegisterAsync_Should_PromoteEnvBoundToExplicit_When_SameActorRegisters()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "pascal", cancellationToken);

        // act
        var result = await _sessions.RegisterAsync(
            generation, "pascal", "orchestrator", "", forceRebind: false, cancellationToken);

        // assert
        Assert.True(result.Changed);
        Assert.Equal(AgentSessionBindingKind.Env, result.PreviousBindingKind);
        Assert.Equal(AgentSessionBindingKind.Explicit, result.Session.BindingKind);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_When_ConflictingActorWithoutForceRebind()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.RegisterAsync(
            generation, "pascal", "orchestrator", "", forceRebind: false, cancellationToken);

        // act & assert
        var exception = await Assert.ThrowsAsync<ExitException>(
            () => _sessions.RegisterAsync(
                generation, "codex", "worker", "", forceRebind: false, cancellationToken));
        Assert.Contains("--force-rebind", exception.Message);
    }

    [Fact]
    public async Task RegisterAsync_Should_RollBackTheIdentityUpsert_When_TheClaimTransitionThrows()
    {
        // arrange: the transaction rollback guarantee - a failed register
        // call (a conflicting actor without --force-rebind) must not leave
        // the durable identity's role changed either, even though the
        // identity upsert runs BEFORE the throwing claim transition inside
        // the same transaction.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.RegisterAsync(
            generation, "pascal", "orchestrator", "", forceRebind: false, cancellationToken);
        await _agentRegistry.RegisterAsync("codex", "original-role", "", cancellationToken);

        // act
        await Assert.ThrowsAsync<ExitException>(
            () => _sessions.RegisterAsync(
                generation, "codex", "new-role", "", forceRebind: false, cancellationToken));

        // assert: codex's identity role is unchanged, and the session is
        // still bound to pascal, not codex.
        var codex = await _agentRegistry.GetAsync("codex", cancellationToken);
        Assert.Equal("original-role", codex!.Role);
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.Equal("pascal", row!.AgentName);
    }

    [Fact]
    public async Task RegisterAsync_Should_RebindAndResetLedger_When_ForceRebindOverridesConflictingActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.RegisterAsync(
            generation, "pascal", "orchestrator", "", forceRebind: false, cancellationToken);
        await InsertDeliveryAsync(generation, "msg-1", cancellationToken);

        // act
        var result = await _sessions.RegisterAsync(
            generation, "codex", "worker", "", forceRebind: true, cancellationToken);

        // assert
        Assert.True(result.Changed);
        Assert.Equal("codex", result.Session.AgentName);
        Assert.Equal("worker", result.Session.Role);
        Assert.Equal(0, await CountDeliveriesAsync(generation, cancellationToken));
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_When_NoRowMatchesTheGeneration()
    {
        // arrange: the missing-row case - no SessionStart hook has fired
        // for this generation yet.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _sessions.RegisterAsync(
                generation, "pascal", "orchestrator", "", forceRebind: false, cancellationToken));
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_When_ThisReaderCannotObserveTheRowsProcessScope()
    {
        // arrange: a positive process-scope mismatch means this reader
        // cannot verify the row's pid refers to the same process its
        // writer observed (e.g. a foreign sandbox), so register must
        // refuse to bind rather than risk a false match.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await SetSessionMetadataAsync(generation, "", "", "pidns:different-scope", cancellationToken);
        var fakeProcessInfo = new FakeProcessInfoProvider { ProcessScope = "pidns:this-reader" };
        fakeProcessInfo.SetAlive(generation.Pid, generation.ProcStart);
        var sessions = NewRegistry(fakeProcessInfo);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => sessions.RegisterAsync(
                generation, "pascal", "orchestrator", "", forceRebind: false, cancellationToken));
    }

    // ---------- FindByProcessAsync ----------

    [Fact]
    public async Task FindByProcessAsync_Should_ReturnEmpty_When_NoRowMatches()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var candidates = await _sessions.FindByProcessAsync(
            AgentSessionHarness.Codex, CurrentHost, 4242, "0", cancellationToken);

        // assert
        Assert.Empty(candidates);
    }

    [Fact]
    public async Task FindByProcessAsync_Should_ReturnOneRow_When_ExactlyOneMatches()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1") with { Harness = AgentSessionHarness.Codex };
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "session-1",
            envActor: null, cancellationToken);

        // act
        var candidates = await _sessions.FindByProcessAsync(
            AgentSessionHarness.Codex, generation.Host, generation.Pid, generation.ProcStart, cancellationToken);

        // assert
        var candidate = Assert.Single(candidates);
        Assert.Equal("session-1", candidate.SessionId);
    }

    [Fact]
    public async Task FindByProcessAsync_Should_ReturnEveryRow_When_MoreThanOneMatches()
    {
        // arrange: an ambiguous match - two distinct session ids happen to
        // share the same (harness, host, pid, proc_start). Should not
        // normally happen, but the caller must be able to detect and
        // refuse it rather than silently pick one.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1") with { Harness = AgentSessionHarness.Codex };
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "session-1",
            envActor: null, cancellationToken);
        var duplicateGeneration = generation with { SessionId = "session-2" };
        await _sessions.StartAsync(
            duplicateGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "session-2",
            envActor: null, cancellationToken);

        // act
        var candidates = await _sessions.FindByProcessAsync(
            AgentSessionHarness.Codex, generation.Host, generation.Pid, generation.ProcStart, cancellationToken);

        // assert
        Assert.Equal(2, candidates.Count);
    }

    [Fact]
    public async Task FindByProcessAsync_Should_ReturnEmpty_When_ProcStartDoesNotMatchAReusedPid()
    {
        // arrange: a stale row from a process that has since exited, whose
        // pid the OS reused for the CURRENT process. The current process's
        // OWN actual start time must not match the stale row's recorded one.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var staleGeneration = DeadGeneration("session-old") with { Harness = AgentSessionHarness.Codex };
        await _sessions.StartAsync(
            staleGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "session-old",
            envActor: null, cancellationToken);
        const string currentProcStart = "123456789";

        // act: the same pid, but a different, current process start time.
        var candidates = await _sessions.FindByProcessAsync(
            AgentSessionHarness.Codex, staleGeneration.Host, staleGeneration.Pid, currentProcStart, cancellationToken);

        // assert
        Assert.Empty(candidates);
    }

    // ---------- FindBySessionIdAsync ----------

    [Fact]
    public async Task FindBySessionIdAsync_Should_ReturnTheRow_When_ItExists()
    {
        // arrange: the authoritative-session-id lookup a sandboxed caller
        // with no live process identity to walk to relies on - it must find
        // the row by (harness, host, session_id) alone.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1") with { Harness = AgentSessionHarness.Codex };
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "session-1",
            envActor: null, cancellationToken);

        // act
        var row = await _sessions.FindBySessionIdAsync(
            AgentSessionHarness.Codex, CurrentHost, "session-1", cancellationToken);

        // assert
        Assert.NotNull(row);
        Assert.Equal(generation.Pid, row.Pid);
        Assert.Equal(generation.ProcStart, row.ProcStart);
    }

    [Fact]
    public async Task FindBySessionIdAsync_Should_ReturnNull_When_NoRowMatches()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var row = await _sessions.FindBySessionIdAsync(
            AgentSessionHarness.Codex, CurrentHost, "session-missing", cancellationToken);

        // assert
        Assert.Null(row);
    }

    [Fact]
    public async Task FindBySessionIdAsync_Should_ReturnNull_When_TheRowBelongsToADifferentHost()
    {
        // arrange: a session id recorded on a different Nitro instance must
        // never be resolved as if it belonged to this one.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var remoteGeneration = AliveGeneration("session-1")
            with
        { Harness = AgentSessionHarness.Codex, Host = RemoteHost };
        await _sessions.StartAsync(
            remoteGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "session-1",
            envActor: null, cancellationToken);

        // act
        var row = await _sessions.FindBySessionIdAsync(
            AgentSessionHarness.Codex, CurrentHost, "session-1", cancellationToken);

        // assert
        Assert.Null(row);
    }

    // ---------- EndAsync ----------

    [Fact]
    public async Task EndAsync_Should_DeleteRowAndCascadeDeliveries_When_GenerationMatches()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(generation, "pascal", forceRebind: false, cancellationToken);
        await InsertDeliveryAsync(generation, "msg-1", cancellationToken);

        // act
        var deleted = await _sessions.EndAsync(generation, cancellationToken);

        // assert
        Assert.True(deleted);
        Assert.Equal(0, await CountSessionRowsAsync(generation, cancellationToken));
        Assert.Equal(0, await CountDeliveriesAsync(generation, cancellationToken));
    }

    [Fact]
    public async Task EndAsync_Should_BeNoOp_When_LateSessionEndTargetsASupersededGeneration()
    {
        // arrange: a TOCTOU-shaped reordering - a session restarts (new
        // generation), and only afterward does a stale SessionEnd for the
        // ORIGINAL generation arrive.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var originalGeneration = AliveGeneration("session-1");
        await _sessions.StartAsync(
            originalGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "pascal", cancellationToken);
        await _sessions.StartAsync(
            originalGeneration with { Pid = DeadPid }, "/work", "/work/.nitro/agents",
            AgentSessionEndpointKind.None, "", envActor: "pascal", cancellationToken);

        // act: the late end targets the pid the row no longer carries.
        var deleted = await _sessions.EndAsync(originalGeneration, cancellationToken);

        // assert
        Assert.False(deleted);
        Assert.Equal(1, await CountSessionRowsAsync(originalGeneration with { Pid = DeadPid }, cancellationToken));
    }

    // ---------- ReapAsync / ListAsync ----------

    [Fact]
    public async Task ReapAsync_Should_DeleteDeadCurrentInstanceRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var deadGeneration = DeadGeneration("session-dead");
        await _sessions.StartAsync(
            deadGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var reaped = await _sessions.ReapAsync(cancellationToken);

        // assert
        Assert.Single(reaped);
        Assert.Equal("session-dead", reaped[0].SessionId);
        Assert.Equal(0, await CountSessionRowsAsync(deadGeneration, cancellationToken));
    }

    [Fact]
    public async Task ReapAsync_Should_NotTouchAliveCurrentInstanceRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var aliveGeneration = AliveGeneration("session-alive");
        await _sessions.StartAsync(
            aliveGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var reaped = await _sessions.ReapAsync(cancellationToken);

        // assert
        Assert.Empty(reaped);
        Assert.Equal(1, await CountSessionRowsAsync(aliveGeneration, cancellationToken));
    }

    [Fact]
    public async Task ReapAsync_Should_NotTouchDeadRemoteRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var remoteDeadGeneration = DeadGeneration("session-remote") with { Host = RemoteHost };
        await _sessions.StartAsync(
            remoteDeadGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var reaped = await _sessions.ReapAsync(cancellationToken);

        // assert
        Assert.Empty(reaped);
        Assert.Equal(1, await CountSessionRowsAsync(remoteDeadGeneration, cancellationToken));
    }

    [Fact]
    public async Task ReapAsync_Should_NotDeleteRow_When_ItWasSupersededByANewerAliveGenerationFirst()
    {
        // arrange: the row started dead (as though the owning process had
        // already exited), but before the reaper runs a fresh SessionStart
        // for the SAME (harness, session_id) replaces it with a live
        // generation. The reaper must never delete the row a newer
        // generation now owns.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var deadGeneration = DeadGeneration("session-1");
        await _sessions.StartAsync(
            deadGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        var aliveGeneration = deadGeneration with
        {
            Pid = CurrentAlivePid(),
            ProcStart = new ProcessInfoProvider().GetStartTicks(CurrentAlivePid())!
        };
        await _sessions.StartAsync(
            aliveGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var reaped = await _sessions.ReapAsync(cancellationToken);

        // assert
        Assert.Empty(reaped);
        Assert.Equal(1, await CountSessionRowsAsync(aliveGeneration, cancellationToken));
    }

    [Fact]
    public async Task ReapAsync_Should_DeleteRow_When_PidIsReused_WithDifferentTicks_DespiteIdenticalWallClockStart()
    {
        // arrange: the exact hazard raw ticks exist to close - a reused pid
        // whose new process happens to start within the same wall-clock
        // instant as the generation it replaced must still be classified
        // dead, because the non-legacy comparison is exact-ticks-string
        // equality with no tolerance at all.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var writerProvider = new ProcessInfoProvider(startTicksReader: _ => "1000000");
        var writer = NewRegistry(writerProvider);
        const int reusedPid = 555_555;
        var generation = new AgentSessionGeneration(
            Harness, "session-1", CurrentHost, reusedPid, writerProvider.GetStartTicks(reusedPid)!);
        await writer.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act: a different process now occupies the same pid, reporting
        // different real kernel start ticks.
        var readerProvider = new ProcessInfoProvider(startTicksReader: _ => "2000000");
        var reader = NewRegistry(readerProvider);
        var reaped = await reader.ReapAsync(cancellationToken);

        // assert
        Assert.Single(reaped);
        Assert.Equal(0, await CountSessionRowsAsync(generation, cancellationToken));
    }

    [Fact]
    public async Task ReapAsync_Should_NotDeleteRow_When_TicksMatchExactly_AcrossTwoIndependentReaders()
    {
        // arrange: two independent ProcessInfoProvider instances, each doing
        // its own real /proc/<pid>/stat read for the same live pid, agree on
        // its raw kernel start ticks exactly.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var writerProvider = new ProcessInfoProvider();
        var writer = NewRegistry(writerProvider);
        var pid = CurrentAlivePid();
        var generation = new AgentSessionGeneration(
            Harness, "session-1", CurrentHost, pid, writerProvider.GetStartTicks(pid)!);
        await writer.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var readerProvider = new ProcessInfoProvider();
        var reader = NewRegistry(readerProvider);
        var reaped = await reader.ReapAsync(cancellationToken);

        // assert
        Assert.Empty(reaped);
        Assert.Equal(1, await CountSessionRowsAsync(generation, cancellationToken));
    }

    [Fact]
    public async Task ListAsync_Should_ComputeStates_When_MixOfOnlineUnreachableAndRemoteRows()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        var online = AliveGeneration("session-online");
        await _sessions.StartAsync(
            online, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.ClaudePeer, "peer-a",
            envActor: null, cancellationToken);

        var unreachable = AliveGeneration("session-unreachable");
        await _sessions.StartAsync(
            unreachable, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        var remote = AliveGeneration("session-remote") with { Host = RemoteHost };
        await _sessions.StartAsync(
            remote, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        var dead = DeadGeneration("session-dead");
        await _sessions.StartAsync(
            dead, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var views = await _sessions.ListAsync(cancellationToken);

        // assert
        Assert.Equal(3, views.Count);
        Assert.Equal(AgentSessionState.Online, views.Single(v => v.Session.SessionId == "session-online").State);
        Assert.Equal(
            AgentSessionState.Unreachable, views.Single(v => v.Session.SessionId == "session-unreachable").State);
        Assert.Equal(AgentSessionState.Remote, views.Single(v => v.Session.SessionId == "session-remote").State);
        Assert.DoesNotContain(views, v => v.Session.SessionId == "session-dead");
    }

    [Fact]
    public async Task ReapAsync_Should_NotDeleteALegacyRow_When_ItsWallClockStartStillMatchesWithinTolerance()
    {
        // arrange: a row a v5-to-v6 migration marked legacy (proc_start is
        // still the pre-v6 DateTimeOffset text) must be read with the old
        // wall-clock-with-tolerance rule, not compared as raw ticks - this
        // process's own live pid and current wall-clock start time, inserted
        // directly since StartAsync only ever writes fresh (non-legacy) rows.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var pid = CurrentAlivePid();
        var wallClockStart = Process.GetCurrentProcess().StartTime.ToUniversalTime().ToString("O");
        await InsertLegacySessionRowAsync("session-legacy-alive", pid, wallClockStart, cancellationToken);

        // act
        var reaped = await _sessions.ReapAsync(cancellationToken);

        // assert
        Assert.Empty(reaped);
        Assert.Equal(
            1,
            await CountSessionRowsAsync(
                new AgentSessionGeneration(Harness, "session-legacy-alive", CurrentHost, pid, wallClockStart),
                cancellationToken));
    }

    [Fact]
    public async Task ReapAsync_Should_DeleteALegacyRow_When_ItsWallClockStartIsOffByMoreThanTolerance()
    {
        // arrange: the legacy tolerance window still closes the pid-reuse
        // hazard it always did - a legacy row whose recorded wall-clock
        // start no longer matches within tolerance is still provably dead.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var pid = CurrentAlivePid();
        var nearMissStart = Process.GetCurrentProcess().StartTime.ToUniversalTime()
            .AddSeconds(5).ToString("O");
        await InsertLegacySessionRowAsync("session-legacy-dead", pid, nearMissStart, cancellationToken);

        // act
        var reaped = await _sessions.ReapAsync(cancellationToken);

        // assert
        Assert.Single(reaped);
        Assert.Equal(
            0,
            await CountSessionRowsAsync(
                new AgentSessionGeneration(Harness, "session-legacy-dead", CurrentHost, pid, nearMissStart),
                cancellationToken));
    }

    [Fact]
    public async Task TouchAsync_Should_ReturnFalse_When_RowIsLegacy_And_GenerationCarriesRawTicks()
    {
        // arrange: a legacy row's proc_start is still the pre-v6
        // DateTimeOffset text; every generation-predicated mutation matches
        // proc_start by SQL equality against raw ticks, so it can never
        // match a legacy row until that row's own next SessionStart
        // rewrites it with fresh ticks.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var pid = CurrentAlivePid();
        var wallClockStart = Process.GetCurrentProcess().StartTime.ToUniversalTime().ToString("O");
        await InsertLegacySessionRowAsync("session-legacy", pid, wallClockStart, cancellationToken);
        var rawTicksGeneration = new AgentSessionGeneration(
            Harness, "session-legacy", CurrentHost, pid, new ProcessInfoProvider().GetStartTicks(pid)!);

        // act
        var touched = await _sessions.TouchAsync(rawTicksGeneration, cancellationToken);

        // assert
        Assert.False(touched);
        Assert.Equal(
            1,
            await CountSessionRowsAsync(
                new AgentSessionGeneration(Harness, "session-legacy", CurrentHost, pid, wallClockStart),
                cancellationToken));
    }

    [Fact]
    public async Task ReapAsync_Should_NotDeleteRow_When_ObservationIsUnobservable()
    {
        // arrange: the registry-level outcome is identical whether the
        // process is unobservable because of a different PID namespace than
        // the row's writer recorded or a permission failure reading the
        // target - both collapse to the same
        // ProcessObservationResult.Unobservable classification, which this
        // reader must never treat as proof of death.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var fakeProcessInfo = new FakeProcessInfoProvider();
        var sessions = NewRegistry(fakeProcessInfo);
        var generation = DeadGeneration("session-1");
        await sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        fakeProcessInfo.SetObservation(generation.Pid, ProcessObservationResult.Unobservable);

        // act
        var reaped = await sessions.ReapAsync(cancellationToken);

        // assert
        Assert.Empty(reaped);
        Assert.Equal(1, await CountSessionRowsAsync(generation, cancellationToken));
    }

    [Fact]
    public async Task ReapAsync_Should_NotDeleteRow_When_TicksReadIsPermissionDenied()
    {
        // arrange: the same Unobservable-not-Dead guarantee, but exercised
        // through the real ProcessInfoProvider (not FakeProcessInfoProvider)
        // so the non-legacy Observe branch itself is under test: an
        // unreadable-but-live pid (e.g. a process running as another user)
        // must not be classified Dead and reaped.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var pid = CurrentAlivePid();
        var permissionDeniedProvider = new ProcessInfoProvider(
            observeReader: (int _, out bool permissionDenied) =>
            {
                permissionDenied = true;
                return null;
            });
        var sessions = NewRegistry(permissionDeniedProvider);
        var generation = new AgentSessionGeneration(
            Harness, "session-1", CurrentHost, pid, new ProcessInfoProvider().GetStartTicks(pid)!);
        await sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var reaped = await sessions.ReapAsync(cancellationToken);

        // assert
        Assert.Empty(reaped);
        Assert.Equal(1, await CountSessionRowsAsync(generation, cancellationToken));
    }

    [Fact]
    public async Task ListAsync_Should_ReportUnobservableState_When_CurrentInstanceRowCannotBeObserved()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var fakeProcessInfo = new FakeProcessInfoProvider();
        var sessions = NewRegistry(fakeProcessInfo);
        var generation = AliveGeneration("session-1");
        await sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.ClaudePeer, "peer-a",
            envActor: null, cancellationToken);
        fakeProcessInfo.SetObservation(generation.Pid, ProcessObservationResult.Unobservable);

        // act
        var views = await sessions.ListAsync(cancellationToken);

        // assert: the row survives (not reaped) and is reported distinctly
        // from online/unreachable/remote.
        var view = Assert.Single(views);
        Assert.Equal(AgentSessionState.Unobservable, view.State);
    }

    // ---------- TouchAsync ----------

    [Fact]
    public async Task TouchAsync_Should_AdvanceLastBeatAt_Without_ChangingOtherFields()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        var started = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(generation, "pascal", forceRebind: false, cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var touched = await _sessions.TouchAsync(generation, cancellationToken);

        // assert
        Assert.True(touched);
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.True(row!.LastBeatAt > started.LastBeatAt);
        Assert.Equal(AgentSessionBindingKind.Explicit, row.BindingKind);
        Assert.Equal("pascal", row.AgentName);
        Assert.Equal(0, row.BlockBudgetUsed);
    }

    [Fact]
    public async Task TouchAsync_Should_ReturnFalse_When_SessionHasAlreadyEnded()
    {
        // arrange: heartbeat-after-end - a late touch for a generation whose
        // row SessionEnd already deleted.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.EndAsync(generation, cancellationToken);

        // act
        var touched = await _sessions.TouchAsync(generation, cancellationToken);

        // assert
        Assert.False(touched);
    }

    [Fact]
    public async Task TouchAsync_Should_ReturnFalse_When_GenerationIsStale()
    {
        // arrange: a generation change superseded the row; a late touch for
        // the OLD generation must not affect the row a newer generation now
        // owns.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var originalGeneration = AliveGeneration("session-1");
        await _sessions.StartAsync(
            originalGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.StartAsync(
            originalGeneration with { Pid = DeadPid }, "/work", "/work/.nitro/agents",
            AgentSessionEndpointKind.None, "", envActor: null, cancellationToken);

        // act
        var touched = await _sessions.TouchAsync(originalGeneration, cancellationToken);

        // assert
        Assert.False(touched);
    }

    // ---------- ListParticipantsAsync ----------

    [Fact]
    public async Task ListParticipantsAsync_Should_ReturnOneRowPerSession_When_MultipleSessionsExist()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var first = AliveGeneration("session-1");
        await _sessions.StartAsync(
            first, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        var second = AliveGeneration("session-2");
        await _sessions.StartAsync(
            second, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var participants = await _sessions.ListParticipantsAsync(cancellationToken);

        // assert
        Assert.Equal(["session-1", "session-2"], participants.Select(p => p.Session.SessionId).Order());
    }

    [Fact]
    public async Task ListParticipantsAsync_Should_JoinDurableAgentIdentity_When_SessionIsClaimed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var claimed = AliveGeneration("session-claimed");
        await _sessions.StartAsync(
            claimed, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.ClaudePeer, "peer-1",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(claimed, "pascal", forceRebind: false, cancellationToken);

        var unclaimed = AliveGeneration("session-unclaimed");
        await _sessions.StartAsync(
            unclaimed, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var participants = await _sessions.ListParticipantsAsync(cancellationToken);

        // assert
        var claimedParticipant = Assert.Single(participants, p => p.Session.SessionId == "session-claimed");
        Assert.Equal("pascal", claimedParticipant.Agent?.Name);
        Assert.Equal(AgentSessionState.Online, claimedParticipant.State);

        var unclaimedParticipant = Assert.Single(participants, p => p.Session.SessionId == "session-unclaimed");
        Assert.Null(unclaimedParticipant.Agent);
    }

    [Fact]
    public async Task ListParticipantsAsync_Should_ResolveSharedAgentIdentity_When_MultipleSessionsBindTheSameAgent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var first = AliveGeneration("session-1");
        await _sessions.StartAsync(
            first, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(first, "pascal", forceRebind: false, cancellationToken);

        var second = AliveGeneration("session-2");
        await _sessions.StartAsync(
            second, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(second, "pascal", forceRebind: false, cancellationToken);

        // act
        var participants = await _sessions.ListParticipantsAsync(cancellationToken);

        // assert
        Assert.Equal(2, participants.Count);
        Assert.All(participants, p => Assert.Equal("pascal", p.Agent?.Name));
    }

    [Fact]
    public async Task ListParticipantsAsync_Should_ReapDeadCurrentInstanceRow_Before_Listing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var dead = DeadGeneration("session-dead");
        await _sessions.StartAsync(
            dead, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var participants = await _sessions.ListParticipantsAsync(cancellationToken);

        // assert
        Assert.Empty(participants);
    }

    // ---------- SelfClaimAsync ----------

    [Fact]
    public async Task SelfClaimAsync_Should_BootstrapAndClaimTheRow_When_AncestorResolverFindsASession()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var pid = CurrentAlivePid();
        var ancestor = new ClaudeAncestorSession(pid, "claude-session-1", _tempRoot.FullName, "peer-a");
        var sessions = new AgentSessionRegistry(
            _fileSystem,
            _timeProvider,
            _database,
            _agentRegistry,
            new FixedInstanceIdProvider(CurrentHost),
            new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName),
            new ProcessInfoProvider(),
            new FixedAncestorSessionResolver(ancestor));

        // act
        var result = await sessions.SelfClaimAsync("pascal", forceRebind: false, cancellationToken);

        // assert
        Assert.True(result.Changed);
        Assert.Equal(AgentSessionHarness.ClaudeCode, result.Session.Harness);
        Assert.Equal("claude-session-1", result.Session.SessionId);
        Assert.Equal("pascal", result.Session.AgentName);
        Assert.Equal(AgentSessionBindingKind.Explicit, result.Session.BindingKind);
        Assert.Equal(AgentSessionEndpointKind.ClaudePeer, result.Session.EndpointKind);
        Assert.Equal("peer-a", result.Session.EndpointAddr);
    }

    [Fact]
    public async Task SelfClaimAsync_Should_Throw_When_NoAncestorSessionIsFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _sessions.SelfClaimAsync("pascal", forceRebind: false, cancellationToken));
    }

    [Fact]
    public async Task SelfClaimAsync_Should_Throw_When_CwdWorkspaceDiffersFromAncestorWorkspace()
    {
        // arrange: this process's own cwd (_tempRoot.FullName, via
        // _fileSystem) resolves to the CURRENT workspace, but the ancestor
        // Claude Code session's cwd resolves to a DIFFERENT, separately
        // initialized workspace.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        var otherRoot = Path.Combine(_tempRoot.FullName, "other-workspace");
        var otherWorkspaceDirectory = AgentWorkspace.GetDirectory(otherRoot);
        Directory.CreateDirectory(otherWorkspaceDirectory);
        await using (await _database.InitializeAsync(otherWorkspaceDirectory, cancellationToken))
        {
        }

        var ancestor = new ClaudeAncestorSession(CurrentAlivePid(), "claude-session-1", otherRoot, "peer-a");
        var sessions = new AgentSessionRegistry(
            _fileSystem,
            _timeProvider,
            _database,
            _agentRegistry,
            new FixedInstanceIdProvider(CurrentHost),
            new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName),
            new ProcessInfoProvider(),
            new FixedAncestorSessionResolver(ancestor));

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => sessions.SelfClaimAsync("pascal", forceRebind: false, cancellationToken));
        Assert.Equal(0, await CountAllSessionRowsAsync(_workspaceDirectory, cancellationToken));
    }

    // ---------- FindLiveClaimedByAgentNameAsync ----------

    [Fact]
    public async Task FindLiveClaimedByAgentNameAsync_Should_ReturnOnlyCurrentHostLiveSessionsClaimedByTheActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        var mine = AliveGeneration("session-mine");
        await _sessions.StartAsync(
            mine, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "thread-1",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(mine, "pascal", forceRebind: false, cancellationToken);

        var someoneElse = AliveGeneration("session-someone-else");
        await _sessions.StartAsync(
            someoneElse, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(someoneElse, "codex", forceRebind: false, cancellationToken);

        var remote = AliveGeneration("session-remote") with { Host = RemoteHost };
        await _sessions.StartAsync(
            remote, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "pascal", cancellationToken);

        var dead = DeadGeneration("session-dead");
        await _sessions.StartAsync(
            dead, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "pascal", cancellationToken);

        // act
        var live = await _sessions.FindLiveClaimedByAgentNameAsync("pascal", cancellationToken);

        // assert: the remote row (never pinged from here) and the dead
        // current-host row (reaped on read) are both excluded, as is
        // codex's own session.
        var row = Assert.Single(live);
        Assert.Equal("session-mine", row.SessionId);
    }

    [Fact]
    public async Task FindLiveClaimedByAgentNameAsync_Should_NotDeleteALiveHostRow_When_ReadFromAForeignScope()
    {
        // arrange: simulates the notifier or `nitro agent ping` running
        // inside a sandbox that cannot see the host pid its own row
        // recorded - a foreign-scope read must still return the row as a
        // ping candidate, not silently delete it as dead.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var fakeProcessInfo = new FakeProcessInfoProvider();
        var sessions = NewRegistry(fakeProcessInfo);
        var generation = AliveGeneration("session-1");
        await sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "thread-1",
            envActor: null, cancellationToken);
        await sessions.ClaimAsync(generation, "pascal", forceRebind: false, cancellationToken);
        fakeProcessInfo.SetObservation(generation.Pid, ProcessObservationResult.Unobservable);

        // act
        var live = await sessions.FindLiveClaimedByAgentNameAsync("pascal", cancellationToken);

        // assert
        Assert.Single(live);
        Assert.Equal(1, await CountSessionRowsAsync(generation, cancellationToken));
    }

    // ---------- TryClaimPingCooldownAsync ----------

    [Fact]
    public async Task TryClaimPingCooldownAsync_Should_Claim_When_NoPriorAttempt()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        var session = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "thread-1",
            envActor: null, cancellationToken);
        var now = _timeProvider.GetUtcNow();

        // act
        var claimed = await _sessions.TryClaimPingCooldownAsync(
            session, "attempt-1", now, TimeSpan.FromSeconds(60), cancellationToken);

        // assert
        Assert.True(claimed);
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.Equal("attempt-1", row!.LastPingAttempt);
        Assert.Null(row.LastPingResult);
        Assert.Equal(now, row.LastPingAt);
    }

    [Fact]
    public async Task TryClaimPingCooldownAsync_Should_ReturnFalse_When_StillWithinCooldown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        var session = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "thread-1",
            envActor: null, cancellationToken);
        var first = _timeProvider.GetUtcNow();
        await _sessions.TryClaimPingCooldownAsync(session, "attempt-1", first, TimeSpan.FromSeconds(60), cancellationToken);
        _timeProvider.Advance(TimeSpan.FromSeconds(30));

        // act
        var claimed = await _sessions.TryClaimPingCooldownAsync(
            session, "attempt-2", _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(60), cancellationToken);

        // assert: coalesced - the row still carries the first attempt.
        Assert.False(claimed);
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.Equal("attempt-1", row!.LastPingAttempt);
    }

    [Fact]
    public async Task TryClaimPingCooldownAsync_Should_ReturnTrue_When_CooldownElapsed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        var session = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "thread-1",
            envActor: null, cancellationToken);
        var first = _timeProvider.GetUtcNow();
        await _sessions.TryClaimPingCooldownAsync(session, "attempt-1", first, TimeSpan.FromSeconds(60), cancellationToken);
        _timeProvider.Advance(TimeSpan.FromSeconds(61));

        // act
        var claimed = await _sessions.TryClaimPingCooldownAsync(
            session, "attempt-2", _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(60), cancellationToken);

        // assert
        Assert.True(claimed);
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.Equal("attempt-2", row!.LastPingAttempt);
    }

    [Fact]
    public async Task TryClaimPingCooldownAsync_Should_ReturnFalse_When_GenerationNoLongerMatches()
    {
        // arrange: the session ended (or rebound to a new generation)
        // between resolution and the cooldown claim.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        var session = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "thread-1",
            envActor: null, cancellationToken);
        await _sessions.EndAsync(generation, cancellationToken);

        // act
        var claimed = await _sessions.TryClaimPingCooldownAsync(
            session, "attempt-1", _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(60), cancellationToken);

        // assert
        Assert.False(claimed);
    }

    [Fact]
    public async Task TryClaimPingCooldownAsync_Should_HaveExactlyOneWinner_When_ConcurrentClaimsRaceTheSameSession()
    {
        // arrange: separate connections racing the same session row - the
        // notifier's required "cooldown holds across concurrent processes"
        // test.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        var session = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "thread-1",
            envActor: null, cancellationToken);
        var now = _timeProvider.GetUtcNow();

        // act
        var results = await Task.WhenAll(Enumerable.Range(1, 5).Select(i =>
            _sessions.TryClaimPingCooldownAsync(
                session, $"attempt-{i}", now, TimeSpan.FromSeconds(60), cancellationToken)));

        // assert
        Assert.Equal(1, results.Count(claimed => claimed));
    }

    // ---------- WritePingResultAsync ----------

    [Fact]
    public async Task WritePingResultAsync_Should_Write_When_AttemptIdMatches()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        var session = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "thread-1",
            envActor: null, cancellationToken);
        await _sessions.TryClaimPingCooldownAsync(
            session, "attempt-1", _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(60), cancellationToken);

        // act
        await _sessions.WritePingResultAsync(
            Harness, "session-1", "attempt-1", AgentPingResult.Ok, null, cancellationToken);

        // assert
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.Equal(AgentPingResult.Ok, row!.LastPingResult);
    }

    [Fact]
    public async Task WritePingResultAsync_Should_BeANoOp_When_AttemptIdIsStale()
    {
        // arrange: an out-of-order completion from an older attempt must
        // never overwrite a newer attempt's result.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = AliveGeneration("session-1");
        var session = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "thread-1",
            envActor: null, cancellationToken);
        await _sessions.TryClaimPingCooldownAsync(
            session, "attempt-1", _timeProvider.GetUtcNow(), TimeSpan.Zero, cancellationToken);
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        await _sessions.TryClaimPingCooldownAsync(
            session, "attempt-2", _timeProvider.GetUtcNow(), TimeSpan.Zero, cancellationToken);

        // act: attempt-1's late completion arrives after attempt-2 already
        // claimed the row.
        await _sessions.WritePingResultAsync(
            Harness, "session-1", "attempt-1", AgentPingResult.Timeout, null, cancellationToken);

        // assert
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.Null(row!.LastPingResult);
        Assert.Equal("attempt-2", row.LastPingAttempt);
    }

    // ---------- helpers ----------

    /// <summary>
    /// A pid guaranteed to be alive for the duration of the test: the test
    /// host process itself, so <see cref="ProcessInfoProvider"/> reports it
    /// alive without any fake process abstraction.
    /// </summary>
    private static int CurrentAlivePid() => Environment.ProcessId;

    /// <summary>
    /// A pid that (barring extraordinary pid-space exhaustion) belongs to no
    /// running process, so <see cref="ProcessInfoProvider"/> reports it dead.
    /// </summary>
    private const int DeadPid = 999_999;

    /// <summary>
    /// An arbitrary raw-ticks-format string: since <see cref="DeadPid"/>
    /// resolves to no running process, <see cref="ProcessInfoProvider"/>
    /// reports it dead regardless of what this value is.
    /// </summary>
    private const string DeadProcStart = "999999999";

    private AgentSessionGeneration AliveGeneration(string sessionId) => new(
        Harness, sessionId, CurrentHost, CurrentAlivePid(),
        new ProcessInfoProvider().GetStartTicks(CurrentAlivePid())!);

    private AgentSessionGeneration DeadGeneration(string sessionId) => new(
        Harness, sessionId, CurrentHost, DeadPid, DeadProcStart);

    /// <summary>
    /// A registry instance identical to <see cref="_sessions"/> except for
    /// its <see cref="IProcessInfoProvider"/>, for scenarios a real OS
    /// process cannot deterministically induce (a different PID namespace,
    /// a permission failure).
    /// </summary>
    private AgentSessionRegistry NewRegistry(IProcessInfoProvider processInfoProvider) => new(
        _fileSystem,
        _timeProvider,
        _database,
        _agentRegistry,
        new FixedInstanceIdProvider(CurrentHost),
        new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName),
        processInfoProvider,
        new FixedAncestorSessionResolver(null));

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }

    private async Task InsertDeliveryAsync(
        AgentSessionGeneration generation, string messageId, CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO session_deliveries (harness, session_id, message_id, channel, delivered_at) "
            + "VALUES ($harness, $sessionId, $messageId, 'digest', $now);";
        command.Parameters.AddWithValue("$harness", generation.Harness);
        command.Parameters.AddWithValue("$sessionId", generation.SessionId);
        command.Parameters.AddWithValue("$messageId", messageId);
        command.Parameters.AddWithValue("$now", _timeProvider.GetUtcNow());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Sets the v5 role/harness_version/process_scope columns directly via
    /// raw SQL, standing in for the not-yet-written production caller (a
    /// later bead) that will populate them.
    /// </summary>
    private async Task SetSessionMetadataAsync(
        AgentSessionGeneration generation,
        string role,
        string harnessVersion,
        string processScope,
        CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE agent_sessions SET role = $role, harness_version = $harnessVersion, "
            + "process_scope = $processScope WHERE harness = $harness AND session_id = $sessionId "
            + "AND pid = $pid AND proc_start = $procStart AND host = $host;";
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$harnessVersion", harnessVersion);
        command.Parameters.AddWithValue("$processScope", processScope);
        command.Parameters.AddWithValue("$harness", generation.Harness);
        command.Parameters.AddWithValue("$sessionId", generation.SessionId);
        command.Parameters.AddWithValue("$pid", generation.Pid);
        command.Parameters.AddWithValue("$procStart", generation.ProcStart);
        command.Parameters.AddWithValue("$host", generation.Host);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts an <c>agent_sessions</c> row directly with
    /// <c>proc_start_legacy = 1</c>, standing in for a row a v5-to-v6
    /// migration carried forward: <see cref="AgentSessionRegistry.StartAsync"/>
    /// only ever writes fresh (non-legacy) rows, so this bypasses it to
    /// reach the legacy state a migration alone produces.
    /// </summary>
    private async Task InsertLegacySessionRowAsync(
        string sessionId, int pid, string procStart, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                proc_start_legacy
            ) VALUES (
                $harness, $sessionId, NULL, 'none', $host, $pid, $procStart,
                '/work', '/work/.nitro/agents', 'none', '', $now, $now, 1
            );
            """;
        command.Parameters.AddWithValue("$harness", Harness);
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$host", CurrentHost);
        command.Parameters.AddWithValue("$pid", pid);
        command.Parameters.AddWithValue("$procStart", procStart);
        command.Parameters.AddWithValue("$now", now);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<long> CountDeliveriesAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM session_deliveries WHERE harness = $harness AND session_id = $sessionId;";
        command.Parameters.AddWithValue("$harness", generation.Harness);
        command.Parameters.AddWithValue("$sessionId", generation.SessionId);

        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task<long> CountSessionRowsAsync(
        AgentSessionGeneration generation, CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM agent_sessions WHERE harness = $harness AND session_id = $sessionId "
            + "AND pid = $pid AND proc_start = $procStart AND host = $host;";
        command.Parameters.AddWithValue("$harness", generation.Harness);
        command.Parameters.AddWithValue("$sessionId", generation.SessionId);
        command.Parameters.AddWithValue("$pid", generation.Pid);
        command.Parameters.AddWithValue("$procStart", generation.ProcStart);
        command.Parameters.AddWithValue("$host", generation.Host);

        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task<long> CountAllSessionRowsAsync(
        string workspaceDirectory, CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM agent_sessions;";

        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }
}

internal sealed class FixedAncestorSessionResolver(ClaudeAncestorSession? session) : IClaudeAncestorSessionResolver
{
    public ClaudeAncestorSession? Resolve() => session;
}

/// <summary>
/// A controllable <see cref="IProcessInfoProvider"/> for liveness scenarios
/// a real OS process cannot deterministically induce: an explicit <see
/// cref="SetObservation"/> pins <see cref="Observe"/>'s result for a pid
/// regardless of its actual liveness. A pid neither registered alive nor
/// pinned observes as <see cref="ProcessObservationResult.Dead"/>.
/// </summary>
internal sealed class FakeProcessInfoProvider : IProcessInfoProvider
{
    private readonly Dictionary<int, string> _aliveStartTicks = [];
    private readonly Dictionary<int, string> _observations = [];

    public string ProcessScope { get; set; } = "";

    public void SetAlive(int pid, string startTicks) => _aliveStartTicks[pid] = startTicks;

    public void SetObservation(int pid, string observation) => _observations[pid] = observation;

    public string? GetStartTicks(int pid)
        => _aliveStartTicks.TryGetValue(pid, out var start) ? start : null;

    public bool IsAlive(int pid, string expectedStartTicks)
        => _aliveStartTicks.TryGetValue(pid, out var start) && start == expectedStartTicks;

    public string GetProcessScope() => ProcessScope;

    public bool CanObserveScope(string recordedProcessScope)
    {
        var readerScope = GetProcessScope();

        return !(readerScope.Length > 0 && recordedProcessScope.Length > 0 && readerScope != recordedProcessScope);
    }

    public string Observe(int pid, string expectedProcStart, bool legacy, string recordedProcessScope)
        => _observations.TryGetValue(pid, out var observation)
            ? observation
            : IsAlive(pid, expectedProcStart) ? ProcessObservationResult.Alive : ProcessObservationResult.Dead;
}
