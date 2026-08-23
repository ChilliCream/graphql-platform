using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="CopilotHookHandler"/> end to end against a real
/// workspace database: presence upsert AND the initial unread-mail digest on
/// SessionStart (spike S5 redo, perles-net-k3j.4, live-verified
/// <c>additionalContext</c> lands there), the documented UserPromptSubmit
/// no-op (S5 redo live-verified that hook's response body is dropped), and
/// conditional teardown on SessionEnd. Every call runs with
/// <c>dryRun: true</c>, mirroring <c>CodexHookHandlerTests</c>.
/// </summary>
public sealed class CopilotHookHandlerTests : IDisposable
{
    private const string SessionId = "b2535577-1f31-4eaa-8688-963b7953a657";

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
    private readonly CopilotHookHandler _handler;

    public CopilotHookHandlerTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-copilot-hook-handler-tests");
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

        _handler = CreateHandler();
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    private CopilotHookHandler CreateHandler() => new(
        _fileSystem,
        _timeProvider,
        _sessions,
        _ledger,
        _mail,
        _environmentVariables,
        new ProcessInfoProvider(),
        new FixedCopilotAncestorSessionResolver(null),
        new FixedInstanceIdProvider("host-1"),
        new FixedGlobalConfigDirectoryProvider(_workspaceRoot));

    // ---------- SessionStart ----------

    [Fact]
    public async Task HandleSessionStartAsync_Should_CreateUnclaimedRow_When_NoEnvActorIsSet()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        Assert.Equal(CopilotHookOutcome.Neutral, outcome);
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
    public async Task HandleSessionStartAsync_Should_RecordNoEndpoint_Always()
    {
        // No Copilot extension exists yet in this ticket's scope
        // (perles-net-k3j.16, sibling task): every row records
        // endpoint_kind = 'none' regardless of whether the session is
        // claimed.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "pascal");

        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.Equal(AgentSessionEndpointKind.None, row.EndpointKind);
        Assert.Equal(string.Empty, row.EndpointAddr);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_ReturnTheInitialDigest_When_UnreadMailExistsForTheClaimedActor()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await SendMailAsync("bob", "alice", cancellationToken);

        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        Assert.NotNull(outcome.AdditionalContext);
        Assert.Contains("1 unread message.", outcome.AdditionalContext);
        Assert.Contains("from bob", outcome.AdditionalContext);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_ReturnNeutral_When_NoEnvActorIsSet()
    {
        // Nothing to claim against yet, so no digest is attempted even
        // though mail may exist for some other actor.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        Assert.Equal(CopilotHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_ReturnNeutral_When_CalledAgainWithNoNewMail()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await SendMailAsync("bob", "alice", cancellationToken);
        var first = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.NotNull(first.AdditionalContext);

        var second = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        Assert.Equal(CopilotHookOutcome.Neutral, second);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_ReturnNeutralWithoutCreatingARow_When_CwdHasNoWorkspace()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var noWorkspaceRoot = Directory.CreateTempSubdirectory("nitro-copilot-hook-no-workspace-tests");

        try
        {
            var payload = new CopilotHookPayload { SessionId = SessionId, Cwd = noWorkspaceRoot.FullName };

            var outcome = await _handler.HandleSessionStartAsync(payload, dryRun: true, cancellationToken);

            Assert.Equal(CopilotHookOutcome.Neutral, outcome);
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
        var payload = new CopilotHookPayload { SessionId = null, Cwd = _workspaceRoot };

        var outcome = await _handler.HandleSessionStartAsync(payload, dryRun: true, cancellationToken);

        Assert.Equal(CopilotHookOutcome.Neutral, outcome);
    }

    // ---------- UserPromptSubmit ----------

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_AlwaysReturnNeutral_EvenWhenUnreadMailExists()
    {
        // Documented no-op: S5 redo live-verified this event's response body
        // is dropped by Copilot 1.0.80, so no digest is attempted and no
        // ledger reservation is made for it.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await SendMailAsync("bob", "alice", cancellationToken);

        var outcome = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);

        Assert.Equal(CopilotHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_NotConsumeTheDigestChannelReservation()
    {
        // Because UserPromptSubmit never reserves, a later SessionStart (or
        // any digest-channel consumer) for the same session must still be
        // able to deliver mail that arrived beforehand.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        _environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
        await SendMailAsync("bob", "alice", cancellationToken);
        await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);

        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        Assert.NotNull(outcome.AdditionalContext);
        Assert.Contains("from bob", outcome.AdditionalContext);
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

        Assert.Equal(CopilotHookOutcome.Neutral, outcome);
        Assert.Null(await FindRowAsync(cancellationToken));
    }

    [Fact]
    public async Task HandleSessionEndAsync_Should_ReturnNeutral_When_NoRowExists()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        var outcome = await _handler.HandleSessionEndAsync(Payload(SessionId), dryRun: true, cancellationToken);

        Assert.Equal(CopilotHookOutcome.Neutral, outcome);
    }

    // ---------- helpers ----------

    private CopilotHookPayload Payload(string sessionId) => new() { SessionId = sessionId, Cwd = _workspaceRoot };

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
        AgentSessionHarness.Copilot,
        SessionId,
        "host-1",
        Pid: 1,
        DateTimeOffset.UnixEpoch);
}

internal sealed class FixedCopilotAncestorSessionResolver(CopilotAncestorSession? session)
    : ICopilotAncestorSessionResolver
{
    public CopilotAncestorSession? Resolve() => session;
}
