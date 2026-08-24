using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="ClaudeHookHandler"/> end to end against a real
/// workspace database: presence upsert on SessionStart, the unread-mail
/// digest and per-turn budget reset on UserPromptSubmit, the Stop gate
/// (reentrancy, per-turn budget, ledger reservation), and conditional
/// teardown on SessionEnd. Every call runs with <c>dryRun: true</c>, which
/// pins the row's generation to the fixed sentinel identity (pid 1,
/// <see cref="DateTimeOffset.UnixEpoch"/> proc_start) instead of walking for
/// a live Claude Code ancestor - the same substitution the command layer's
/// <c>--dry-run</c> flag makes for fixture-driven runs.
/// </summary>
public sealed class ClaudeHookHandlerTests : IDisposable
{
    private const string SessionId = "session-1";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentDatabase _database;
    private readonly AgentRegistry _agentRegistry;
    private readonly AgentSessionRegistry _sessions;
    private readonly SessionDeliveryLedger _ledger;
    private readonly MailStore _mail;
    private readonly FixedEnvironmentVariableProvider _environmentVariables;
    private readonly ClaudeHookHandler _handler;

    public ClaudeHookHandlerTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-claude-hook-handler-tests");
        _workspaceRoot = _tempRoot.FullName;
        _workspaceDirectory = AgentWorkspace.GetDirectory(_workspaceRoot);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_workspaceRoot);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        _database = new AgentDatabase();
        _agentRegistry = new AgentRegistry(_fileSystem, _timeProvider, _database);
        _sessions = new AgentSessionRegistry(
            _fileSystem,
            _timeProvider,
            _database,
            _agentRegistry,
            new FixedInstanceIdProvider("host-1"),
            new FixedGlobalConfigDirectoryProvider(_workspaceRoot),
            new ProcessInfoProvider(),
            new FixedAncestorSessionResolver(null));
        _ledger = new SessionDeliveryLedger(_fileSystem, _database);
        _mail = new MailStore(_fileSystem, _timeProvider, _database, _agentRegistry);
        _environmentVariables = new FixedEnvironmentVariableProvider();

        _handler = new ClaudeHookHandler(
            _fileSystem,
            _timeProvider,
            _sessions,
            _ledger,
            _mail,
            _environmentVariables,
            new ProcessInfoProvider(),
            new FixedAncestorSessionResolver(null),
            new FixedClaudeHarnessVersionResolver(),
            new FixedInstanceIdProvider("host-1"),
            new FixedGlobalConfigDirectoryProvider(_workspaceRoot));
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    private ClaudeHookHandler CreateHandler() => new(
        _fileSystem,
        _timeProvider,
        _sessions,
        _ledger,
        _mail,
        _environmentVariables,
        new ProcessInfoProvider(),
        new FixedAncestorSessionResolver(null),
        new FixedClaudeHarnessVersionResolver(),
        new FixedInstanceIdProvider("host-1"),
        new FixedGlobalConfigDirectoryProvider(_workspaceRoot));

    // ---------- SessionStart ----------

    [Fact]
    public async Task HandleSessionStartAsync_Should_CreateUnclaimedRow_When_NoEnvActorIsSet()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.Equal(AgentSessionBindingKind.None, row.BindingKind);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_BindTheRow_When_NitroMailActorIsSet()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "pascal");

        // act
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.Equal("pascal", row.AgentName);
        Assert.Equal(AgentSessionBindingKind.Env, row.BindingKind);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_ReturnNeutralWithoutCreatingARow_When_CwdHasNoWorkspace()
    {
        // arrange: fail-open on a missing workspace. The payload's cwd is a
        // separate temp root with no agents.db anywhere in its ancestry, so
        // AgentWorkspace.Find resolves nothing (unlike a subdirectory of the
        // real workspace, which Find would still resolve by walking up).
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var noWorkspaceRoot = Directory.CreateTempSubdirectory("nitro-claude-hook-no-workspace-tests");

        try
        {
            var payload = new ClaudeHookPayload { SessionId = SessionId, Cwd = noWorkspaceRoot.FullName };

            // act
            var outcome = await _handler.HandleSessionStartAsync(payload, dryRun: true, cancellationToken);

            // assert
            Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
            Assert.Null(await FindRowAsync(cancellationToken));
        }
        finally
        {
            noWorkspaceRoot.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_ReturnNeutral_When_SessionIdIsMissing()
    {
        // arrange: fail-open on a malformed/incomplete payload.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var payload = new ClaudeHookPayload { SessionId = null, Cwd = _workspaceRoot };

        // act
        var outcome = await _handler.HandleSessionStartAsync(payload, dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_RecordHarnessVersion_When_TheResolverReturnsOne()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var handler = new ClaudeHookHandler(
            _fileSystem,
            _timeProvider,
            _sessions,
            _ledger,
            _mail,
            _environmentVariables,
            new ProcessInfoProvider(),
            new FixedAncestorSessionResolver(null),
            new FixedClaudeHarnessVersionResolver("2.1.241"),
            new FixedInstanceIdProvider("host-1"),
            new FixedGlobalConfigDirectoryProvider(_workspaceRoot));

        // act
        await handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.Equal("2.1.241", row!.HarnessVersion);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_LeaveHarnessVersionBlank_When_TheResolverReturnsNone()
    {
        // arrange: a metadata resolution failure (no session file, a
        // reused pid, or a malformed file) must never block session
        // creation.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.Equal("", row!.HarnessVersion);
    }

    // ---------- UserPromptSubmit ----------

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_AdvanceLastBeatAt_When_GenerationResolves()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var before = (await FindRowAsync(cancellationToken))!.LastBeatAt;
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        var after = (await FindRowAsync(cancellationToken))!.LastBeatAt;
        Assert.True(after > before);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ReturnNeutral_When_SessionIsUnclaimed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // act
        var outcome = await _handler.HandleUserPromptSubmitAsync(
            Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ReturnDigest_When_UnreadMailExistsForTheClaimedActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("bob", "alice", cancellationToken);

        // act
        var outcome = await _handler.HandleUserPromptSubmitAsync(
            Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.NotNull(outcome.AdditionalContext);
        Assert.Contains("1 unread message.", outcome.AdditionalContext);
        Assert.Contains("from bob", outcome.AdditionalContext);
        Assert.False(outcome.Block);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ReturnNeutral_When_CalledAgainWithNoNewMail()
    {
        // arrange: the ledger suppresses redelivery of the same message on
        // the digest channel once it has been reserved.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("bob", "alice", cancellationToken);
        var first = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.NotNull(first.AdditionalContext);

        // act
        var second = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, second);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_NotRedeliver_When_AMessageIsMarkedUnreadAfterItsDigest()
    {
        // arrange: the ledger's suppression is about NOTIFICATION, not read
        // state - marking a delivered message unread again must not cause
        // the digest to show it a second time.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var message = await SendMailAsync("bob", "alice", cancellationToken);
        var first = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.NotNull(first.AdditionalContext);
        await _mail.MarkUnreadAsync([message.Id], "alice", cancellationToken);

        // act
        var second = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, second);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ResetTheBlockBudget()
    {
        // arrange: drive the Stop gate's budget to its ceiling first.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        for (var i = 0; i < ClaudeHookHandler.MaxBlocksPerTurn; i++)
        {
            await SendMailAsync($"bob-{i}", "alice", cancellationToken);
            await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);
        }

        var exhaustedRow = await FindRowAsync(cancellationToken);
        Assert.Equal(ClaudeHookHandler.MaxBlocksPerTurn, exhaustedRow!.BlockBudgetUsed);

        // act
        await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        var resetRow = await FindRowAsync(cancellationToken);
        Assert.Equal(0, resetRow!.BlockBudgetUsed);
    }

    // ---------- Stop ----------

    [Fact]
    public async Task HandleStopAsync_Should_AdvanceLastBeatAt_When_GenerationResolves()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var before = (await FindRowAsync(cancellationToken))!.LastBeatAt;
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        var after = (await FindRowAsync(cancellationToken))!.LastBeatAt;
        Assert.True(after > before);
    }

    [Fact]
    public async Task HandleStopAsync_Should_ReturnNeutral_When_StopHookActiveIsTrue()
    {
        // arrange: the reentrancy guard fires before anything else, even
        // when unread mail exists.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("bob", "alice", cancellationToken);

        // act
        var outcome = await _handler.HandleStopAsync(
            Payload(SessionId, stopHookActive: true), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleStopAsync_Should_Block_When_UnreadMailExistsForTheClaimedActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("bob", "alice", cancellationToken);

        // act
        var outcome = await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.True(outcome.Block);
        Assert.NotNull(outcome.BlockReason);
    }

    [Fact]
    public async Task HandleStopAsync_Should_ReturnNeutral_When_CalledAgainForTheSameUnreadMail()
    {
        // arrange: the gate channel's ledger reservation is at-most-once per
        // message, so a second Stop for the exact same still-unread mail
        // does not block again.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("bob", "alice", cancellationToken);
        var first = await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.True(first.Block);

        // act
        var second = await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, second);
    }

    [Fact]
    public async Task HandleStopAsync_Should_StopBlocking_When_PerTurnBudgetIsExhausted()
    {
        // arrange: each iteration sends a NEW message so every Stop call has
        // fresh, never-gated mail to react to; only the budget should stop
        // the blocking, not the ledger.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        for (var i = 0; i < ClaudeHookHandler.MaxBlocksPerTurn; i++)
        {
            await SendMailAsync($"bob-{i}", "alice", cancellationToken);
            var outcome = await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);
            Assert.True(outcome.Block);
        }

        await SendMailAsync("bob-over-budget", "alice", cancellationToken);

        // act: budget is now exhausted.
        var overBudget = await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, overBudget);
    }

    [Fact]
    public async Task HandleStopAsync_Should_LeaveTheMessageEligibleForAFutureBudgetCycle_When_OverBudget()
    {
        // arrange: exhaust the budget on unrelated mail, leaving one message
        // never actually gated because every Stop call was over budget by
        // the time it was considered.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        for (var i = 0; i < ClaudeHookHandler.MaxBlocksPerTurn; i++)
        {
            await SendMailAsync($"bob-{i}", "alice", cancellationToken);
            await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);
        }

        var pending = await SendMailAsync("bob-pending", "alice", cancellationToken);
        await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken); // over budget, no-op

        // act: a fresh turn resets the budget, so the message the previous
        // turn never got to gate must still be eligible.
        await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var afterReset = await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.True(afterReset.Block);
        Assert.NotNull(pending.Id);
    }

    [Fact]
    public async Task HandleStopAsync_Should_ReturnNeutral_When_TheRowIsDeletedBetweenResolveAndIncrement()
    {
        // arrange: the increment reports no row matched (e.g. a concurrent
        // SessionEnd deleted it after FindByGenerationAsync above already
        // saw it), so the ledger reservation must not be reported as a
        // block the caller never actually recorded a budget spend for.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("bob", "alice", cancellationToken);

        var handler = new ClaudeHookHandler(
            _fileSystem,
            _timeProvider,
            new IncrementNeverMatchesAgentSessionRegistry(_sessions),
            _ledger,
            _mail,
            _environmentVariables,
            new ProcessInfoProvider(),
            new FixedAncestorSessionResolver(null),
            new FixedClaudeHarnessVersionResolver(),
            new FixedInstanceIdProvider("host-1"),
            new FixedGlobalConfigDirectoryProvider(_workspaceRoot));

        // act
        var outcome = await handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleStopAsync_Should_ReserveAtMostMaxDigestMessages_When_ManyMessagesAreUnread()
    {
        // arrange: the Stop path's unbounded inbox query would otherwise
        // reserve every unread message for the gate channel even though a
        // single block is emitted regardless of how many there are.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        for (var i = 0; i < ClaudeHookHandler.MaxDigestMessages + 5; i++)
        {
            await SendMailAsync($"bob-{i}", "alice", cancellationToken);
        }

        var spyLedger = new ReserveCapturingSessionDeliveryLedger(_ledger);
        var handler = new ClaudeHookHandler(
            _fileSystem,
            _timeProvider,
            _sessions,
            spyLedger,
            _mail,
            _environmentVariables,
            new ProcessInfoProvider(),
            new FixedAncestorSessionResolver(null),
            new FixedClaudeHarnessVersionResolver(),
            new FixedInstanceIdProvider("host-1"),
            new FixedGlobalConfigDirectoryProvider(_workspaceRoot));

        // act
        var outcome = await handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.True(outcome.Block);
        Assert.NotNull(spyLedger.LastMessageIds);
        Assert.True(spyLedger.LastMessageIds!.Count <= ClaudeHookHandler.MaxDigestMessages);
    }

    [Fact]
    public async Task HandleStopAsync_Should_ResolveTheSameGeneration_When_ReplayedFromADifferentHandlerInstance()
    {
        // arrange: dry-run pins a fixed sentinel identity rather than this
        // process's own pid and start time, so a session-start captured by
        // one handler instance and replayed against a second (a separate CLI
        // invocation, in real usage) still resolves the same generation
        // instead of minting an unrelated row.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        var sessionStartHandler = CreateHandler();
        await sessionStartHandler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("bob", "alice", cancellationToken);

        // act
        var stopHandler = CreateHandler();
        var outcome = await stopHandler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.True(outcome.Block);
        Assert.NotNull(outcome.BlockReason);
    }

    // ---------- SessionEnd ----------

    [Fact]
    public async Task HandleSessionEndAsync_Should_DeleteTheRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.NotNull(await FindRowAsync(cancellationToken));

        // act
        var outcome = await _handler.HandleSessionEndAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
        Assert.Null(await FindRowAsync(cancellationToken));
    }

    [Fact]
    public async Task HandleSessionEndAsync_Should_ReturnNeutral_When_NoRowExists()
    {
        // arrange: fail-open, no SessionStart ever ran for this session.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var outcome = await _handler.HandleSessionEndAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
    }

    // ---------- helpers ----------

    private ClaudeHookPayload Payload(string sessionId, bool stopHookActive = false) => new()
    {
        SessionId = sessionId,
        Cwd = _workspaceRoot,
        StopHookActive = stopHookActive
    };

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }

    private async Task<MailMessage> SendMailAsync(string sender, string recipient, CancellationToken cancellationToken)
        => await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = sender, Subject = "status", Body = "please check", To = [recipient] },
            cancellationToken);

    private async Task<AgentSessionRecord?> FindRowAsync(CancellationToken cancellationToken)
        => await _sessions.FindByGenerationAsync(CurrentGeneration(), cancellationToken);

    private static AgentSessionGeneration CurrentGeneration() => new(
        AgentSessionHarness.ClaudeCode,
        SessionId,
        "host-1",
        Pid: 1,
        DateTimeOffset.UnixEpoch);
}

internal sealed class FixedAncestorSessionResolver(ClaudeAncestorSession? session) : IClaudeAncestorSessionResolver
{
    public ClaudeAncestorSession? Resolve() => session;
}

internal sealed class FixedClaudeHarnessVersionResolver(string version = "") : IClaudeHarnessVersionResolver
{
    public string Resolve(int pid) => version;
}

internal sealed class FixedCodexHarnessVersionResolver(string version = "") : ICodexHarnessVersionResolver
{
    public string Resolve(string sessionId, int ancestorPid) => version;
}

internal sealed class FixedCopilotHarnessVersionResolver(string version = "") : ICopilotHarnessVersionResolver
{
    public string Resolve(string sessionId, int ancestorPid) => version;
}

/// <summary>
/// Wraps a real <see cref="IAgentSessionRegistry"/>, delegating every member
/// except <see cref="IncrementBlockBudgetAsync"/>, which always reports no
/// row matched - simulating a row deleted (SessionEnd) between an earlier
/// <see cref="FindByGenerationAsync"/> and the increment.
/// </summary>
internal sealed class IncrementNeverMatchesAgentSessionRegistry(IAgentSessionRegistry inner) : IAgentSessionRegistry
{
    public Task<AgentSessionRecord> StartAsync(
        AgentSessionGeneration generation,
        string cwd,
        string workspacePath,
        string endpointKind,
        string endpointAddr,
        string? envActor,
        CancellationToken cancellationToken)
        => inner.StartAsync(generation, cwd, workspacePath, endpointKind, endpointAddr, envActor, cancellationToken);

    public Task<AgentSessionClaimResult> ClaimAsync(
        AgentSessionGeneration generation, string actor, bool forceRebind, CancellationToken cancellationToken)
        => inner.ClaimAsync(generation, actor, forceRebind, cancellationToken);

    public Task<AgentSessionClaimResult> SelfClaimAsync(
        string actor, bool forceRebind, CancellationToken cancellationToken)
        => inner.SelfClaimAsync(actor, forceRebind, cancellationToken);

    public Task<bool> EndAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.EndAsync(generation, cancellationToken);

    public Task<AgentSessionRecord?> FindByGenerationAsync(
        AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.FindByGenerationAsync(generation, cancellationToken);

    public Task ResetBlockBudgetAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.ResetBlockBudgetAsync(generation, cancellationToken);

    public Task<int?> IncrementBlockBudgetAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => Task.FromResult<int?>(null);

    public Task<IReadOnlyList<AgentSessionRecord>> ReapAsync(CancellationToken cancellationToken)
        => inner.ReapAsync(cancellationToken);

    public Task<IReadOnlyList<AgentSessionView>> ListAsync(CancellationToken cancellationToken)
        => inner.ListAsync(cancellationToken);

    public Task<bool> TouchAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.TouchAsync(generation, cancellationToken);

    public Task<bool> RecordHarnessVersionAsync(
        AgentSessionGeneration generation, string harnessVersion, CancellationToken cancellationToken)
        => inner.RecordHarnessVersionAsync(generation, harnessVersion, cancellationToken);

    public Task<IReadOnlyList<AgentSessionRecord>> FindLiveClaimedByAgentNameAsync(
        string agentName, CancellationToken cancellationToken)
        => inner.FindLiveClaimedByAgentNameAsync(agentName, cancellationToken);

    public Task<bool> TryClaimPingCooldownAsync(
        AgentSessionRecord session,
        string attemptId,
        DateTimeOffset now,
        TimeSpan cooldown,
        CancellationToken cancellationToken)
        => inner.TryClaimPingCooldownAsync(session, attemptId, now, cooldown, cancellationToken);

    public Task WritePingResultAsync(
        string harness,
        string sessionId,
        string attemptId,
        string result,
        string? detail,
        CancellationToken cancellationToken)
        => inner.WritePingResultAsync(harness, sessionId, attemptId, result, detail, cancellationToken);
}

/// <summary>
/// Wraps a real <see cref="ISessionDeliveryLedger"/>, delegating every call
/// while capturing the <c>messageIds</c> argument of the most recent
/// <see cref="ReserveAsync"/> call.
/// </summary>
internal sealed class ReserveCapturingSessionDeliveryLedger(ISessionDeliveryLedger inner) : ISessionDeliveryLedger
{
    public IReadOnlyList<string>? LastMessageIds { get; private set; }

    public Task<IReadOnlyList<string>> ReserveAsync(
        string harness,
        string sessionId,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
    {
        LastMessageIds = messageIds;
        return inner.ReserveAsync(harness, sessionId, messageIds, channel, deliveredAt, cancellationToken);
    }
}
