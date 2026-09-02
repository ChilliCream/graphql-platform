using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="CodexHookHandler"/> end to end against a real
/// workspace database: presence upsert on SessionStart, the unread-mail
/// digest on UserPromptSubmit, conditional teardown on SessionEnd, and the
/// notify-driven idle-turn gate - including the S2-verified notify/queue
/// loop guard (a message already claimed on the gate channel is never
/// re-queued when the queued digest's own delivery turn re-fires notify).
/// Every call runs with <c>dryRun: true</c>, mirroring
/// <c>ClaudeHookHandlerTests</c>.
/// </summary>
public sealed class CodexHookHandlerTests : IDisposable
{
    private const string SessionId = "01a02e51-c257-75c3-b242-b56199a18839";

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
    private readonly FakeCodexQueueClient _queueClient;
    private readonly CodexHookHandler _handler;

    public CodexHookHandlerTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-codex-hook-handler-tests");
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
            new FixedGlobalConfigDirectoryProvider(_workspaceRoot));
        _ledger = new SessionDeliveryLedger(_fileSystem, _database);
        _mail = new MailStore(_fileSystem, _timeProvider, _database, _agentRegistry);
        _environmentVariables = new FixedEnvironmentVariableProvider();
        _queueClient = new FakeCodexQueueClient();

        _handler = CreateHandler();
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    private CodexHookHandler CreateHandler() => new(
        _fileSystem,
        _timeProvider,
        _sessions,
        _ledger,
        _mail,
        _environmentVariables,
        new FixedCodexHarnessVersionResolver(),
        new FixedInstanceIdProvider("host-1"),
        new FixedGlobalConfigDirectoryProvider(_workspaceRoot),
        _queueClient);

    // ---------- SessionStart ----------

    [Fact]
    public async Task HandleSessionStartAsync_Should_BindTheRowToAGeneratedActor_When_NoEnvActorIsSet()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert: startup assigns a friendly actor and injects it into the
        // harness context immediately.
        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.Contains($"Your Nitro actor name is \"{row.AgentName}\".", outcome.AdditionalContext);
        Assert.DoesNotContain(SessionId, row.AgentName);
        Assert.Equal(AgentSessionBindingKind.Explicit, row.BindingKind);
        Assert.Equal("", row.Role);
        Assert.NotNull(await _agentRegistry.GetAsync(row.AgentName!, cancellationToken));
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_BindTheSameGeneratedActor_When_CalledAgainForTheSameSession()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var first = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.Equal(first.AdditionalContext, outcome.AdditionalContext);
        Assert.Contains($"Your Nitro actor name is \"{row.AgentName}\".", outcome.AdditionalContext);
        Assert.Equal(AgentSessionBindingKind.Explicit, row.BindingKind);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_SetTheCodexThreadEndpoint_ToTheSessionId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.Equal(AgentSessionEndpointKind.CodexThread, row.EndpointKind);
        Assert.Equal(SessionId, row.EndpointAddr);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_ReturnNeutralWithoutCreatingARow_When_CwdHasNoWorkspace()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var noWorkspaceRoot = Directory.CreateTempSubdirectory("nitro-codex-hook-no-workspace-tests");

        try
        {
            var payload = new CodexHookPayload { SessionId = SessionId, Cwd = noWorkspaceRoot.FullName };

            var outcome = await _handler.HandleSessionStartAsync(payload, dryRun: true, cancellationToken);

            Assert.Equal(CodexHookOutcome.Neutral, outcome);
            Assert.Null(await FindRowAsync(cancellationToken));
        }
        finally
        {
            noWorkspaceRoot.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_ReturnNeutral_When_CwdIsMissing()
    {
        // arrange: fail-open on a malformed/incomplete payload - no process
        // identity's workspace can even be checked without a cwd.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var payload = new CodexHookPayload { SessionId = SessionId, Cwd = null };

        var outcome = await _handler.HandleSessionStartAsync(payload, dryRun: true, cancellationToken);

        Assert.Equal(CodexHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_NotCreateAProvisionalIdentity_When_SessionIdIsMissing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var payload = new CodexHookPayload { SessionId = null, Cwd = _workspaceRoot };

        // act
        var first = await _handler.HandleSessionStartAsync(payload, dryRun: true, cancellationToken);
        var second = await _handler.HandleSessionStartAsync(payload, dryRun: true, cancellationToken);

        // assert
        Assert.Equal(CodexHookOutcome.Neutral, first);
        Assert.Equal(CodexHookOutcome.Neutral, second);
        Assert.Equal(0L, await CountAllSessionRowsAsync(cancellationToken));
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_RecordHarnessVersion_When_TheResolverReturnsOne()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var handler = new CodexHookHandler(
            _fileSystem,
            _timeProvider,
            _sessions,
            _ledger,
            _mail,
            _environmentVariables,
            new FixedCodexHarnessVersionResolver("0.101.0"),
            new FixedInstanceIdProvider("host-1"),
            new FixedGlobalConfigDirectoryProvider(_workspaceRoot),
            _queueClient);

        await handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        var row = await FindRowAsync(cancellationToken);
        Assert.Equal("0.101.0", row!.HarnessVersion);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_LeaveHarnessVersionBlank_When_TheResolverReturnsNone()
    {
        // A metadata resolution failure (no rollout file, no resolvable exe
        // path) must never block session creation.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        var row = await FindRowAsync(cancellationToken);
        Assert.Equal("", row!.HarnessVersion);
    }

    // ---------- UserPromptSubmit ----------

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_AdvanceLastBeatAt_When_GenerationResolves()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var before = (await FindRowAsync(cancellationToken))!.LastBeatAt;
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);

        var after = (await FindRowAsync(cancellationToken))!.LastBeatAt;
        Assert.True(after > before);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ReturnNeutral_When_NoMailIsAddressedToTheActor()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        var outcome = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);

        Assert.Equal(CodexHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ReturnDigest_When_UnreadMailExistsForTheClaimedActor()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);

        var outcome = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);

        Assert.NotNull(outcome.AdditionalContext);
        Assert.Contains("1 unread nitro message.", outcome.AdditionalContext);
        Assert.Contains("nitro agent mail inbox --actor", outcome.AdditionalContext);
        Assert.DoesNotContain("Your Nitro actor name", outcome.AdditionalContext);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ReturnNeutral_When_CalledAgainWithNoNewMail()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        var first = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.NotNull(first.AdditionalContext);

        var second = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);

        Assert.Equal(CodexHookOutcome.Neutral, second);
    }

    // ---------- SessionEnd ----------

    [Fact]
    public async Task HandleSessionEndAsync_Should_DeleteTheRow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.NotNull(await FindRowAsync(cancellationToken));

        var outcome = await _handler.HandleSessionEndAsync(Payload(SessionId), dryRun: true, cancellationToken);

        Assert.Equal(CodexHookOutcome.Neutral, outcome);
        Assert.Null(await FindRowAsync(cancellationToken));
    }

    [Fact]
    public async Task HandleSessionEndAsync_Should_ReturnNeutral_When_NoRowExists()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        var outcome = await _handler.HandleSessionEndAsync(Payload(SessionId), dryRun: true, cancellationToken);

        Assert.Equal(CodexHookOutcome.Neutral, outcome);
    }

    // ---------- Notify (the idle-turn gate) ----------

    [Fact]
    public async Task HandleNotifyAsync_Should_AdvanceLastBeatAt_When_GenerationResolves()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var before = (await FindRowAsync(cancellationToken))!.LastBeatAt;
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        await _handler.HandleNotifyAsync(NotifyPayload(SessionId), dryRun: true, cancellationToken);

        var after = (await FindRowAsync(cancellationToken))!.LastBeatAt;
        Assert.True(after > before);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_ReturnNeutral_When_TypeIsNotAgentTurnComplete()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);

        var outcome = await _handler.HandleNotifyAsync(
            NotifyPayload(SessionId, type: "something-else"), dryRun: true, cancellationToken);

        Assert.Equal(CodexNotifyOutcome.Neutral, outcome);
        Assert.Empty(_queueClient.Calls);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_ReturnNeutral_When_NoMailIsAddressedToTheGeneratedActor()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        var outcome = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), dryRun: true, cancellationToken);

        Assert.Equal(CodexNotifyOutcome.Neutral, outcome);
        Assert.Empty(_queueClient.Calls);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_QueueTheDigestJson_When_UnreadMailExistsForTheClaimedActor()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var message = await SendMailAsync("bob", actor, cancellationToken);

        var outcome = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), dryRun: true, cancellationToken);

        Assert.True(outcome.Queued);
        var call = Assert.Single(_queueClient.Calls);
        Assert.Equal(SessionId, call.ThreadId);
        Assert.Contains("1 shown below as `nitro agent mail read --thread --output json` prints them.", call.Message);
        Assert.Contains(message.Id, call.Message);
        Assert.Contains("\"items\"", call.Message);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_QueueTheInboxPointer_When_MailWasAnnouncedOnTheDigestChannel()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // act
        var outcome = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.True(outcome.Queued);
        var call = Assert.Single(_queueClient.Calls);
        Assert.Equal(
            $"You have 1 unread nitro message. Run `nitro agent mail inbox --actor {actor}`.",
            call.Message);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_NotReQueue_When_TheQueuedDigestsOwnDeliveryTurnRefiresNotify()
    {
        // arrange: turn 1
        // fires notify, queues a digest; turn 2 (the queued digest's own
        // delivery) fires notify again for the SAME thread-id with a new
        // turn-id, and the message-id-keyed ledger must skip it, otherwise
        // the notify/queue loop never terminates.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);

        var first = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), dryRun: true, cancellationToken);
        Assert.True(first.Queued);

        // act: second notify firing, same thread, no NEW mail.
        var second = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(CodexNotifyOutcome.Neutral, second);
        Assert.Single(_queueClient.Calls);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_QueueAgain_When_NewMailArrivesAfterAnEarlierQueue()
    {
        // The gate channel's ledger reservation is at-most-once PER MESSAGE,
        // not per session: a brand new message must still gate even after an
        // earlier message was already queued and delivered.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        await _handler.HandleNotifyAsync(NotifyPayload(SessionId), dryRun: true, cancellationToken);

        await SendMailAsync("carol", actor, cancellationToken);
        var outcome = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), dryRun: true, cancellationToken);

        Assert.True(outcome.Queued);
        Assert.Equal(2, _queueClient.Calls.Count);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_ReturnNeutral_When_QueueClientFails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        _queueClient.NextResult = CodexQueueResult.Error;

        var outcome = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), dryRun: true, cancellationToken);

        Assert.False(outcome.Queued);

        // The ledger reservation still stands: a retried notify for the SAME
        // (already-attempted) mail does not queue it a second time. This is
        // the plan's documented reserve-then-emit crash policy.
        var retried = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), dryRun: true, cancellationToken);
        Assert.Equal(CodexNotifyOutcome.Neutral, retried);
        Assert.Single(_queueClient.Calls);
    }

    // ---------- helpers ----------

    private CodexHookPayload Payload(string sessionId) => new() { SessionId = sessionId, Cwd = _workspaceRoot };

    private CodexNotifyPayload NotifyPayload(string threadId, string type = CodexNotifyPayload.AgentTurnComplete)
        => new() { Type = type, ThreadId = threadId, Cwd = _workspaceRoot };

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

    private async Task<string> StartAndGetActorAsync(CancellationToken cancellationToken)
    {
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var row = await FindRowAsync(cancellationToken);

        return row!.AgentName!;
    }

    private async Task<AgentSessionRecord?> FindRowAsync(CancellationToken cancellationToken)
        => await _sessions.FindByGenerationAsync(CurrentGeneration(), cancellationToken);

    private async Task<long> CountAllSessionRowsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM agent_sessions;";

        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static AgentSessionGeneration CurrentGeneration() => new(
        AgentSessionHarness.Codex,
        SessionId,
        "host-1");
}

internal sealed class FakeCodexQueueClient : ICodexQueueClient
{
    public List<(string ThreadId, string Message)> Calls { get; } = [];

    public CodexQueueResult NextResult { get; set; } = CodexQueueResult.Ok;

    public Task<CodexQueueResult> QueueAsync(string threadId, string message, CancellationToken cancellationToken)
    {
        Calls.Add((threadId, message));
        return Task.FromResult(NextResult);
    }
}
