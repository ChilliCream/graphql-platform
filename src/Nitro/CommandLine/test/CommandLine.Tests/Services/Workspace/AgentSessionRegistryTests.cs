using System.Diagnostics;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="AgentSessionRegistry"/>'s lifecycle: SessionStart
/// binding rules and same/different-generation handling, the v5
/// role/harness_version columns defaulting on creation,
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

    /// <summary>
    /// A second Nitro instance id, for the same (harness, session id) seen
    /// under a generation this instance does not own.
    /// </summary>
    private const string OtherHost = "host-other";

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
            new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName));
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    // ---------- StartAsync ----------

    [Fact]
    public async Task StartAsync_Should_AssignActor_When_NoEnvActorGiven()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = Generation("session-1");

        // act
        var record = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // assert
        Assert.Contains(record.AgentName, AgentActorAllocator.BaseActors);
        Assert.Equal(AgentSessionBindingKind.Explicit, record.BindingKind);
    }

    [Fact]
    public async Task StartAsync_Should_CreateEnvBoundRow_When_EnvActorGiven()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = Generation("session-1");

        // act
        var record = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "pascal", cancellationToken);

        // assert
        Assert.Equal("pascal", record.AgentName);
        Assert.Equal(AgentSessionBindingKind.Env, record.BindingKind);
    }

    [Fact]
    public async Task StartAsync_Should_UseEveryBaseActorBeforeSuffixingTheNextWave()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actors = new List<string>();

        for (var i = 0; i <= AgentActorAllocator.BaseActors.Count; i++)
        {
            var record = await _sessions.StartAsync(
                Generation($"session-{i}"),
                "/work",
                "/work/.nitro/agents",
                AgentSessionEndpointKind.None,
                "",
                envActor: null,
                cancellationToken);
            actors.Add(record.AgentName!);
        }

        Assert.Equal(
            AgentActorAllocator.BaseActors.Order(StringComparer.Ordinal),
            actors.Take(AgentActorAllocator.BaseActors.Count).Order(StringComparer.Ordinal));
        Assert.EndsWith("-1", actors[^1], StringComparison.Ordinal);
        Assert.Contains(actors[^1][..^2], AgentActorAllocator.BaseActors);
    }

    [Fact]
    public async Task StartAsync_Should_ReconcileLiveRow_When_LegacySessionHasNoIdentity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = Generation("session-legacy");
        await _sessions.StartAsync(
            generation,
            "/work",
            "/work/.nitro/agents",
            AgentSessionEndpointKind.None,
            "",
            envActor: "legacy-actor",
            cancellationToken);
        await InsertDeliveryAsync(generation, "message-old", cancellationToken);

        await using (var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken))
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                "DELETE FROM agent_session_identities WHERE harness = $harness AND session_id = $sessionId";
            command.Parameters.AddWithValue("$harness", generation.Harness);
            command.Parameters.AddWithValue("$sessionId", generation.SessionId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var reconciled = await _sessions.StartAsync(
            generation,
            "/work",
            "/work/.nitro/agents",
            AgentSessionEndpointKind.None,
            "",
            envActor: null,
            cancellationToken);

        Assert.Contains(reconciled.AgentName, AgentActorAllocator.BaseActors);
        Assert.Equal(0, await CountDeliveriesAsync(generation, cancellationToken));
        var identity = Assert.Single(await _sessions.ListIdentitiesAsync(cancellationToken));
        Assert.Equal(reconciled.AgentName, identity.Identity.Actor);
    }

    [Fact]
    public async Task StartAsync_Should_DefaultRoleAndHarnessVersion_When_RowIsCreated()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = Generation("session-1");

        // act
        var record = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // assert: role and harness_version are not captured yet (a later
        Assert.Equal("", record.Role);
        Assert.Equal("", record.HarnessVersion);
    }

    [Fact]
    public async Task FindByGenerationAsync_Should_RoundTripRoleAndHarnessVersion()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = Generation("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await SetSessionMetadataAsync(generation, "frontend", "1.2.3-beta", cancellationToken);

        // act
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);

        // assert
        Assert.Equal("frontend", row!.Role);
        Assert.Equal("1.2.3-beta", row.HarnessVersion);
    }

    [Fact]
    public async Task StartAsync_Should_PreserveHarnessMetadataAndRole_When_SameGenerationDuplicateSessionStart()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = Generation("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await SetSessionMetadataAsync(generation, "backend", "2.1.0", cancellationToken);

        // act: a duplicate SessionStart for the exact same generation must
        // not reset metadata captured after the row was first created.
        var record = await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // assert
        Assert.Equal("backend", record.Role);
        Assert.Equal("2.1.0", record.HarnessVersion);
    }

    [Fact]
    public async Task StartAsync_Should_ResetHarnessMetadataAndRole_When_GenerationChanges()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var firstGeneration = Generation("session-1");
        await _sessions.StartAsync(
            firstGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await SetSessionMetadataAsync(firstGeneration, "backend", "2.1.0", cancellationToken);

        var restartedGeneration = firstGeneration with { Host = OtherHost };

        // act: a new process replaced the one the row remembered - metadata
        // observed under the OLD generation must not leak into the new one.
        var record = await _sessions.StartAsync(
            restartedGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // assert: role and harness_version reset to blank (not re-captured
        Assert.Equal("", record.Role);
        Assert.Equal("", record.HarnessVersion);
    }

    [Fact]
    public async Task StartAsync_Should_PreserveState_When_SameGenerationDuplicateSessionStart()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = Generation("session-1");
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
        var firstGeneration = Generation("session-1");
        await _sessions.StartAsync(
            firstGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "pascal", cancellationToken);
        await _sessions.ClaimAsync(firstGeneration, "pascal", forceRebind: false, cancellationToken);
        await InsertDeliveryAsync(firstGeneration, "msg-1", cancellationToken);

        var restartedGeneration = firstGeneration with { Host = OtherHost };

        // act: a new process replaced the one the row remembered, under the
        // SAME (harness, session_id).
        var record = await _sessions.StartAsync(
            restartedGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "someone-else", cancellationToken);

        // assert
        Assert.Equal("pascal", record.AgentName);
        Assert.Equal(AgentSessionBindingKind.Explicit, record.BindingKind);
        Assert.Equal(0, record.BlockBudgetUsed);
        Assert.Equal(0, await CountDeliveriesAsync(firstGeneration, cancellationToken));
    }

    // ---------- ClaimAsync state machine ----------

    [Fact]
    public async Task ClaimAsync_Should_Bind_When_NoneToExplicit()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = Generation("session-1");
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
        var generation = Generation("session-1");
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
        var generation = Generation("session-1");
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
        var generation = Generation("session-1");
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
        var generation = Generation("session-1");
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
        var generation = Generation("session-1");
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
        var generation = Generation("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        var staleGeneration = generation with { Host = OtherHost };

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
        await _agentRegistry.EnsureImplicitAsync("pascal", cancellationToken);
        var generation = Generation("session-1");
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
        await _agentRegistry.EnsureImplicitAsync("pascal", cancellationToken);
        var generation = Generation("session-1");
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
        await _agentRegistry.EnsureImplicitAsync("pascal", cancellationToken);
        var generation = Generation("session-1");
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
        await _agentRegistry.EnsureImplicitAsync("pascal", cancellationToken);
        var generation = Generation("session-1");
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
        await _agentRegistry.EnsureImplicitAsync("pascal", cancellationToken);
        var generation = Generation("session-1");
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
        await _agentRegistry.EnsureImplicitAsync("codex", cancellationToken);
        await _agentRegistry.EnsureImplicitAsync("pascal", cancellationToken);
        var generation = Generation("session-1");
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
        await _agentRegistry.EnsureImplicitAsync("codex", cancellationToken);
        await _agentRegistry.EnsureImplicitAsync("pascal", cancellationToken);
        var generation = Generation("session-1");
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
        await _agentRegistry.EnsureImplicitAsync("codex", cancellationToken);
        await _agentRegistry.EnsureImplicitAsync("pascal", cancellationToken);
        var generation = Generation("session-1");
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
        await _agentRegistry.EnsureImplicitAsync("pascal", cancellationToken);
        var generation = Generation("session-1");

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _sessions.RegisterAsync(
                generation, "pascal", "orchestrator", "", forceRebind: false, cancellationToken));
    }

    // ---------- FindByProcessAsync ----------

    // ---------- FindBySessionIdAsync ----------

    [Fact]
    public async Task FindBySessionIdAsync_Should_ReturnTheRow_When_ItExists()
    {
        // arrange: the authoritative-session-id lookup a sandboxed caller
        // with no live process identity to walk to relies on - it must find
        // the row by (harness, host, session_id) alone.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = Generation("session-1") with { Harness = AgentSessionHarness.Codex };
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "session-1",
            envActor: null, cancellationToken);

        // act
        var row = await _sessions.FindBySessionIdAsync(
            AgentSessionHarness.Codex, CurrentHost, "session-1", cancellationToken);

        // assert
        Assert.NotNull(row);
        Assert.Equal(generation.Host, row.Host);
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
        var remoteGeneration = Generation("session-1")
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
        var generation = Generation("session-1");
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
        var originalGeneration = Generation("session-1");
        await _sessions.StartAsync(
            originalGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "pascal", cancellationToken);
        await _sessions.StartAsync(
            originalGeneration with { Host = OtherHost }, "/work", "/work/.nitro/agents",
            AgentSessionEndpointKind.None, "", envActor: "pascal", cancellationToken);

        // act: the late end targets a generation the row no longer carries.
        var deleted = await _sessions.EndAsync(originalGeneration, cancellationToken);

        // assert
        Assert.False(deleted);
        Assert.Equal(1, await CountSessionRowsAsync(originalGeneration with { Host = OtherHost }, cancellationToken));
    }

    // ---------- ReapAsync / ListAsync ----------

    [Fact]
    public async Task ReapAsync_Should_DeleteStaleCurrentInstanceRow()
    {
        // arrange: a row that has not beaten since well before the stale
        // window, which is what a harness that ended without its SessionEnd
        // hook running leaves behind.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var deadGeneration = Generation("session-dead");
        await _sessions.StartAsync(
            deadGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        _timeProvider.Advance(TimeSpan.FromDays(2));

        // act
        var reaped = await _sessions.ReapAsync(cancellationToken);

        // assert
        Assert.Single(reaped);
        Assert.Equal("session-dead", reaped[0].SessionId);
        Assert.Equal(0, await CountSessionRowsAsync(deadGeneration, cancellationToken));
    }

    [Fact]
    public async Task ReapAsync_Should_NotTouchFreshlyBeatenCurrentInstanceRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var aliveGeneration = Generation("session-alive");
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
    public async Task ReapAsync_Should_NotTouchStaleRemoteRow()
    {
        // arrange: stale, but recorded by a different Nitro instance, which
        // this reader never reaps on that instance's behalf.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var remoteDeadGeneration = Generation("session-remote") with { Host = RemoteHost };
        await _sessions.StartAsync(
            remoteDeadGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        _timeProvider.Advance(TimeSpan.FromDays(2));

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
        var deadGeneration = Generation("session-1");
        await _sessions.StartAsync(
            deadGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        var aliveGeneration = deadGeneration with { Host = OtherHost };
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
    public async Task ListAsync_Should_ComputeStates_When_MixOfOnlineUnreachableAndRemoteRows()
    {
        // arrange: the stale row is started first and left behind by the
        // clock, so only it falls outside the reaper's window.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        var dead = Generation("session-dead");
        await _sessions.StartAsync(
            dead, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        _timeProvider.Advance(TimeSpan.FromDays(2));

        var online = Generation("session-online");
        await _sessions.StartAsync(
            online, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.ClaudePeer, "peer-a",
            envActor: null, cancellationToken);

        var unreachable = Generation("session-unreachable");
        await _sessions.StartAsync(
            unreachable, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        var remote = Generation("session-remote") with { Host = RemoteHost };
        await _sessions.StartAsync(
            remote, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
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

    // ---------- TouchAsync ----------

    [Fact]
    public async Task TouchAsync_Should_AdvanceLastBeatAt_Without_ChangingOtherFields()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = Generation("session-1");
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
        var generation = Generation("session-1");
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
        var originalGeneration = Generation("session-1");
        await _sessions.StartAsync(
            originalGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.StartAsync(
            originalGeneration with { Host = OtherHost }, "/work", "/work/.nitro/agents",
            AgentSessionEndpointKind.None, "", envActor: null, cancellationToken);

        // act
        var touched = await _sessions.TouchAsync(originalGeneration, cancellationToken);

        // assert
        Assert.False(touched);
    }

    // ---------- SetRoleAsync ----------

    [Fact]
    public async Task SetRoleAsync_Should_SetTheSessionsOwnRole_Without_TouchingBindingOrCounters()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = Generation("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: "pascal", cancellationToken);

        // act
        var updated = await _sessions.SetRoleAsync(generation, "operator", cancellationToken);

        // assert
        Assert.True(updated);
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.Equal("operator", row!.Role);
        Assert.Equal(AgentSessionBindingKind.Env, row.BindingKind);
        Assert.Equal("pascal", row.AgentName);
    }

    [Fact]
    public async Task SetRoleAsync_Should_Normalize_TheSameWayAgentRoleNormalizeDoes()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = Generation("session-1");
        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        await _sessions.SetRoleAsync(generation, "  Operator  ", cancellationToken);

        // assert
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.Equal("operator", row!.Role);
    }

    [Fact]
    public async Task SetRoleAsync_Should_ReturnFalse_When_GenerationMatchesNoRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = Generation("session-missing");

        // act
        var updated = await _sessions.SetRoleAsync(generation, "operator", cancellationToken);

        // assert
        Assert.False(updated);
    }

    // ---------- ListParticipantsAsync ----------

    [Fact]
    public async Task ListParticipantsAsync_Should_ReturnOneRowPerSession_When_MultipleSessionsExist()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var first = Generation("session-1");
        await _sessions.StartAsync(
            first, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        var second = Generation("session-2");
        await _sessions.StartAsync(
            second, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var participants = await _sessions.ListParticipantsAsync(cancellationToken);

        // assert
        Assert.Equal(["session-1", "session-2"], participants.Select(p => p.Session.SessionId).Order());
    }

    [Fact]
    public async Task ListParticipantsAsync_Should_JoinAssignedActorForEveryCodingSession()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var claimed = Generation("session-claimed");
        await _sessions.StartAsync(
            claimed, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.ClaudePeer, "peer-1",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(claimed, "pascal", forceRebind: false, cancellationToken);

        var unclaimed = Generation("session-unclaimed");
        await _sessions.StartAsync(
            unclaimed, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var participants = await _sessions.ListParticipantsAsync(cancellationToken);

        // assert
        var claimedParticipant = Assert.Single(participants, p => p.Session.SessionId == "session-claimed");
        Assert.Equal("pascal", claimedParticipant.Agent?.Name);
        Assert.Equal(AgentSessionState.Online, claimedParticipant.State);

        var assignedParticipant = Assert.Single(participants, p => p.Session.SessionId == "session-unclaimed");
        Assert.Equal(assignedParticipant.Session.AgentName, assignedParticipant.Agent?.Name);
    }

    [Fact]
    public async Task ListParticipantsAsync_Should_ResolveSharedAgentIdentity_When_MultipleSessionsBindTheSameAgent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var first = Generation("session-1");
        await _sessions.StartAsync(
            first, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(first, "pascal", forceRebind: false, cancellationToken);

        var second = Generation("session-2");
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
    public async Task ListParticipantsAsync_Should_ReapStaleCurrentInstanceRow_Before_Listing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var dead = Generation("session-dead");
        await _sessions.StartAsync(
            dead, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        _timeProvider.Advance(TimeSpan.FromDays(2));

        // act
        var participants = await _sessions.ListParticipantsAsync(cancellationToken);

        // assert
        Assert.Empty(participants);
    }

    // ---------- SelfClaimAsync ----------

    // ---------- FindLiveClaimedByAgentNameAsync ----------

    [Fact]
    public async Task FindLiveClaimedByAgentNameAsync_Should_ReturnOnlyCurrentHostLiveSessionsClaimedByTheActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        var mine = Generation("session-mine");
        await _sessions.StartAsync(
            mine, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "thread-1",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(mine, "pascal", forceRebind: false, cancellationToken);

        var someoneElse = Generation("session-someone-else");
        await _sessions.StartAsync(
            someoneElse, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);
        await _sessions.ClaimAsync(someoneElse, "codex", forceRebind: false, cancellationToken);

        var remote = Generation("session-remote") with { Host = RemoteHost };
        await _sessions.StartAsync(
            remote, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        var dead = Generation("session-dead");
        await _sessions.StartAsync(
            dead, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.None, "",
            envActor: null, cancellationToken);

        // act
        var live = await _sessions.FindLiveClaimedByAgentNameAsync("pascal", cancellationToken);

        // assert: the remote row (never pinged from here) and the dead
        // current-host row (reaped on read) are both excluded, as is
        // codex's own session.
        var row = Assert.Single(live);
        Assert.Equal("session-mine", row.SessionId);
    }

    // ---------- TryClaimPingCooldownAsync ----------

    [Fact]
    public async Task TryClaimPingCooldownAsync_Should_Claim_When_NoPriorAttempt()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = Generation("session-1");
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
        var generation = Generation("session-1");
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
        var generation = Generation("session-1");
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
        var generation = Generation("session-1");
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
        var generation = Generation("session-1");
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
        var generation = Generation("session-1");
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
        var generation = Generation("session-1");
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

    private static AgentSessionGeneration Generation(string sessionId)
        => new(Harness, sessionId, CurrentHost);

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
    /// Sets the v5 role and harness_version columns directly via raw SQL,
    /// standing in for the production callers that populate them.
    /// </summary>
    private async Task SetSessionMetadataAsync(
        AgentSessionGeneration generation,
        string role,
        string harnessVersion,
        CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE agent_sessions SET role = $role, harness_version = $harnessVersion "
            + "WHERE harness = $harness AND session_id = $sessionId AND host = $host;";
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$harnessVersion", harnessVersion);
        command.Parameters.AddWithValue("$harness", generation.Harness);
        command.Parameters.AddWithValue("$sessionId", generation.SessionId);
        command.Parameters.AddWithValue("$host", generation.Host);

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
            + "AND host = $host;";
        command.Parameters.AddWithValue("$harness", generation.Harness);
        command.Parameters.AddWithValue("$sessionId", generation.SessionId);
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
