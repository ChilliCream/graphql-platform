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
            new FixedGlobalConfigDirectoryProvider(_workspaceRoot),
            new ProcessInfoProvider(),
            new FixedAncestorSessionResolver(null));
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
        new ProcessInfoProvider(),
        new FixedCodexAncestorSessionResolver(null),
        new FixedInstanceIdProvider("host-1"),
        new FixedGlobalConfigDirectoryProvider(_workspaceRoot),
        _queueClient);

    // ---------- SessionStart ----------

    [Fact]
    public async Task HandleSessionStartAsync_Should_CreateUnclaimedRow_When_NoEnvActorIsSet()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        Assert.Equal(CodexHookOutcome.Neutral, outcome);
        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.Equal(AgentSessionBindingKind.None, row.BindingKind);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_BindTheRow_When_NitroMailActorIsSet()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "pascal");

        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.Equal("pascal", row.AgentName);
        Assert.Equal(AgentSessionBindingKind.Env, row.BindingKind);
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
    public async Task HandleSessionStartAsync_Should_ReturnNeutral_When_SessionIdIsMissing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var payload = new CodexHookPayload { SessionId = null, Cwd = _workspaceRoot };

        var outcome = await _handler.HandleSessionStartAsync(payload, dryRun: true, cancellationToken);

        Assert.Equal(CodexHookOutcome.Neutral, outcome);
    }

    // ---------- UserPromptSubmit ----------

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ReturnNeutral_When_SessionIsUnclaimed()
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
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("bob", "alice", cancellationToken);

        var outcome = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);

        Assert.NotNull(outcome.AdditionalContext);
        Assert.Contains("1 unread message.", outcome.AdditionalContext);
        Assert.Contains("from bob", outcome.AdditionalContext);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ReturnNeutral_When_CalledAgainWithNoNewMail()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("bob", "alice", cancellationToken);
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
    public async Task HandleNotifyAsync_Should_ReturnNeutral_When_TypeIsNotAgentTurnComplete()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("bob", "alice", cancellationToken);

        var outcome = await _handler.HandleNotifyAsync(
            NotifyPayload(SessionId, type: "something-else"), dryRun: true, cancellationToken);

        Assert.Equal(CodexNotifyOutcome.Neutral, outcome);
        Assert.Empty(_queueClient.Calls);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_ReturnNeutral_When_SessionIsUnclaimed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        var outcome = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), dryRun: true, cancellationToken);

        Assert.Equal(CodexNotifyOutcome.Neutral, outcome);
        Assert.Empty(_queueClient.Calls);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_QueueTheDigest_When_UnreadMailExistsForTheClaimedActor()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("bob", "alice", cancellationToken);

        var outcome = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), dryRun: true, cancellationToken);

        Assert.True(outcome.Queued);
        var call = Assert.Single(_queueClient.Calls);
        Assert.Equal(SessionId, call.ThreadId);
        Assert.Contains("from bob", call.Message);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_NotReQueue_When_TheQueuedDigestsOwnDeliveryTurnRefiresNotify()
    {
        // arrange: reproduces spike S2's captured end-to-end trace - turn 1
        // fires notify, queues a digest; turn 2 (the queued digest's own
        // delivery) fires notify again for the SAME thread-id with a new
        // turn-id, and the message-id-keyed ledger must skip it, otherwise
        // the notify/queue loop never terminates.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("bob", "alice", cancellationToken);

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
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("bob", "alice", cancellationToken);
        await _handler.HandleNotifyAsync(NotifyPayload(SessionId), dryRun: true, cancellationToken);

        await SendMailAsync("carol", "alice", cancellationToken);
        var outcome = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), dryRun: true, cancellationToken);

        Assert.True(outcome.Queued);
        Assert.Equal(2, _queueClient.Calls.Count);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_ReturnNeutral_When_QueueClientFails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("bob", "alice", cancellationToken);
        _queueClient.NextResult = false;

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

    private async Task<AgentSessionRecord?> FindRowAsync(CancellationToken cancellationToken)
        => await _sessions.FindByGenerationAsync(CurrentGeneration(), cancellationToken);

    private static AgentSessionGeneration CurrentGeneration() => new(
        AgentSessionHarness.Codex,
        SessionId,
        "host-1",
        Pid: 1,
        DateTimeOffset.UnixEpoch);
}

internal sealed class FixedCodexAncestorSessionResolver(CodexAncestorSession? session) : ICodexAncestorSessionResolver
{
    public CodexAncestorSession? Resolve() => session;
}

internal sealed class FakeCodexQueueClient : ICodexQueueClient
{
    public List<(string ThreadId, string Message)> Calls { get; } = [];

    public bool NextResult { get; set; } = true;

    public Task<bool> QueueAsync(string threadId, string message, CancellationToken cancellationToken)
    {
        Calls.Add((threadId, message));
        return Task.FromResult(NextResult);
    }
}
