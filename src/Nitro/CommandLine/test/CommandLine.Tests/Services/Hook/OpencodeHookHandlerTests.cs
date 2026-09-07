using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises the opencode hook lifecycle against a real workspace database.
/// </summary>
public sealed class OpencodeHookHandlerTests : IDisposable
{
    private const string SessionId = "ses_01a02e51c25775c3b242b56199a18839";

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
    private readonly OpencodeHookHandler _handler;

    public OpencodeHookHandlerTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-opencode-hook-handler-tests");
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
        _handler = new OpencodeHookHandler(
            _fileSystem,
            _timeProvider,
            _sessions,
            _ledger,
            _mail,
            _environmentVariables,
            new FixedInstanceIdProvider("host-1"),
            new FixedGlobalConfigDirectoryProvider(_workspaceRoot));
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task HandleSessionCreatedAsync_Should_StoreTheOpencodeEndpointVersionAndPassword()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        await _handler.HandleSessionCreatedAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.Equal(AgentSessionEndpointKind.OpencodeServer, row.EndpointKind);
        Assert.Equal("http://127.0.0.1:4096", row.EndpointAddr);
        Assert.Equal("secret", row.EndpointSecret);
        Assert.Equal("1.18.25", row.HarnessVersion);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_AppendTheActorAnnouncementOnlyOnce()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionCreatedAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // act
        var first = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var second = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Single(first.Parts);
        Assert.Contains("Your Nitro actor name is", first.Parts[0]);
        Assert.Equal(OpencodeHookOutcome.Neutral, second);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_AppendTheUnreadMailDigest()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);

        // act
        var outcome = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(2, outcome.Parts.Count);
        Assert.Contains("Your Nitro actor name is", outcome.Parts[0]);
        Assert.Contains("1 unread nitro message.", outcome.Parts[1]);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_ResetThePerTurnBudget()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionCreatedAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await _sessions.IncrementBlockBudgetAsync(CurrentGeneration(), cancellationToken);

        // act
        await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.Equal(0, row!.BlockBudgetUsed);
    }

    [Fact]
    public async Task HandleSessionIdleAsync_Should_DeliverOnlyOnceUntilAnOrdinaryChatMessageRearmsTheTransition()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);

        // act
        var first = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var second = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("carol", actor, cancellationToken);
        var third = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var fourth = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.NotNull(first.IdleDelivery);
        Assert.Equal(OpencodeHookOutcome.Neutral, second);
        Assert.Equal(OpencodeHookOutcome.Neutral, third);
        Assert.NotNull(fourth.IdleDelivery);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_NotRearmOrAppendParts_When_TheMessageWasPushedByNitro()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await _sessions.IncrementBlockBudgetAsync(CurrentGeneration(), cancellationToken);
        var pushedPayload = Payload(SessionId);
        pushedPayload.NitroPushed = true;

        // act
        var pushed = await _handler.HandleChatMessageAsync(
            pushedPayload, dryRun: true, cancellationToken);
        await SendMailAsync("carol", actor, cancellationToken);
        var idle = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, pushed);
        Assert.Equal(OpencodeHookOutcome.Neutral, idle);
        Assert.Equal(1, (await FindRowAsync(cancellationToken))!.BlockBudgetUsed);
    }

    [Fact]
    public async Task HandleSessionIdleAsync_Should_ClaimOneDelivery_When_ConcurrentIdleEventsRace()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);

        // act
        var outcomes = await Task.WhenAll(
            _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken),
            _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken));

        // assert
        Assert.Equal(1, outcomes.Count(static outcome => outcome.IdleDelivery is not null));
    }

    [Fact]
    public async Task HandleSessionIdleAsync_Should_RearmTheTransition_When_NoMailDigestWasReserved()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var firstMail = await SendMailAsync("bob", actor, cancellationToken);
        await _ledger.ReserveAsync(
            CurrentGeneration(),
            [firstMail.Id],
            AgentSessionChannel.Gate,
            _timeProvider.GetUtcNow(),
            cancellationToken);

        // act
        var first = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("carol", actor, cancellationToken);
        var second = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, first);
        Assert.NotNull(second.IdleDelivery);
    }

    [Fact]
    public async Task HandleSessionIdleAsync_Should_NotReserveDelivery_When_HooksAreSuppressed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        _environmentVariables.Set("NITRO_HOOK_SUPPRESS", "1");

        // act
        var suppressed = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);
        _environmentVariables.Set("NITRO_HOOK_SUPPRESS", "0");
        var resumed = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, suppressed);
        Assert.NotNull(resumed.IdleDelivery);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_RemainNeutral_When_TheSessionIsDeletedBeforeReservation()
    {
        // arrange: the first chat message already claimed the announcement,
        // so only the unread-mail digest reservation is left to race the
        // session's deletion below.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        var handler = CreateHandler(new SessionDeletingDeliveryLedger(_ledger, _sessions, CurrentGeneration()));

        // act
        var outcome = await handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, outcome);
        Assert.Null(await FindRowAsync(cancellationToken));
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_RemainNeutral_When_TheSessionWasReplacedBeforeTouch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionCreatedAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await _sessions.StartAsync(
            new AgentSessionGeneration(AgentSessionHarness.Opencode, SessionId, "host-2"),
            _workspaceRoot,
            _workspaceDirectory,
            AgentSessionEndpointKind.OpencodeServer,
            "http://127.0.0.1:4096",
            endpointSecret: null,
            envActor: null,
            cancellationToken);

        // act
        var outcome = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleSessionIdleAsync_Should_RemainNeutral_When_TheSessionWasReplacedBeforeTouch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionCreatedAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await _sessions.StartAsync(
            new AgentSessionGeneration(AgentSessionHarness.Opencode, SessionId, "host-2"),
            _workspaceRoot,
            _workspaceDirectory,
            AgentSessionEndpointKind.OpencodeServer,
            "http://127.0.0.1:4096",
            endpointSecret: null,
            envActor: null,
            cancellationToken);

        // act
        var outcome = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleSessionIdleAsync_Should_RemainNeutral_When_TheSessionIsDeletedBeforeReservation()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        var handler = CreateHandler(new SessionDeletingDeliveryLedger(_ledger, _sessions, CurrentGeneration()));

        // act
        var outcome = await handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, outcome);
        Assert.Null(await FindRowAsync(cancellationToken));
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_AnnounceAgain_When_TheSessionIsCreatedAfterDeletion()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionCreatedAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await _handler.HandleSessionDeletedAsync(Payload(SessionId), dryRun: true, cancellationToken);
        await _handler.HandleSessionCreatedAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // act
        var outcome = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        var announcement = Assert.Single(outcome.Parts);
        Assert.Contains("Your Nitro actor name is", announcement);
    }

    [Fact]
    public void OpencodeHookPayload_Should_ReadTheShimFieldNames()
    {
        // arrange
        var json = OpencodeHookFixtures.Read("session-created.json");

        // act
        var payload = JsonSerializer.Deserialize(json, OpencodeHookJsonContext.Default.OpencodeHookPayload);

        // assert
        Assert.NotNull(payload);
        Assert.Equal(SessionId, payload.SessionId);
        Assert.Equal("http://127.0.0.1:4096", payload.ServerUrl);
        Assert.Equal("secret", payload.ServerPassword);
        Assert.Equal("1.18.25", payload.HarnessVersion);
    }

    private OpencodeHookPayload Payload(string sessionId) => new()
    {
        SessionId = sessionId,
        Cwd = _workspaceRoot,
        ServerUrl = "http://127.0.0.1:4096",
        ServerPassword = "secret",
        HarnessVersion = "1.18.25"
    };

    private OpencodeHookHandler CreateHandler(ISessionDeliveryLedger ledger) => new(
        _fileSystem,
        _timeProvider,
        _sessions,
        ledger,
        _mail,
        _environmentVariables,
        new FixedInstanceIdProvider("host-1"),
        new FixedGlobalConfigDirectoryProvider(_workspaceRoot));

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }

    private async Task<string> StartAndGetActorAsync(CancellationToken cancellationToken)
    {
        await _handler.HandleSessionCreatedAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var row = await FindRowAsync(cancellationToken);

        return row!.AgentName!;
    }

    private Task<MailMessage> SendMailAsync(string sender, string recipient, CancellationToken cancellationToken)
        => _mail.SendMessageAsync(
            new MailMessageCreation { Sender = sender, Subject = "status", Body = "please check", To = [recipient] },
            cancellationToken);

    private Task<AgentSessionRecord?> FindRowAsync(CancellationToken cancellationToken)
        => _sessions.FindByGenerationAsync(CurrentGeneration(), cancellationToken);

    private static AgentSessionGeneration CurrentGeneration() => new(
        AgentSessionHarness.Opencode,
        SessionId,
        "host-1");
}

internal sealed class SessionDeletingDeliveryLedger(
    ISessionDeliveryLedger inner,
    IAgentSessionRegistry sessionRegistry,
    AgentSessionGeneration generation) : ISessionDeliveryLedger
{
    private bool _deleted;

    public Task<IReadOnlyList<string>> ReserveAsync(
        string harness,
        string sessionId,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
        => inner.ReserveAsync(harness, sessionId, messageIds, channel, deliveredAt, cancellationToken);

    public async Task<IReadOnlyList<string>> ReserveAsync(
        AgentSessionGeneration reserveGeneration,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
    {
        if (!_deleted)
        {
            _deleted = true;
            await sessionRegistry.EndAsync(generation, cancellationToken);
        }

        return await inner.ReserveAsync(
            reserveGeneration,
            messageIds,
            channel,
            deliveredAt,
            cancellationToken);
    }
}

internal static class OpencodeHookFixtures
{
    public static string Read(string fileName, [System.Runtime.CompilerServices.CallerFilePath] string sourceFile = "")
    {
        var directory = Path.GetDirectoryName(sourceFile)!;
        var path = Path.Combine(directory, "..", "..", "..", "fixtures", "hooks", "opencode", fileName);

        return File.ReadAllText(path);
    }
}
