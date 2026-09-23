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
        _mail = new MailStore(
            _fileSystem, _timeProvider, _database, new AgentStore(_fileSystem, _timeProvider, _database));
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

    /// <summary>
    /// A session reporting <c>serverBound: false</c> registers no push endpoint
    /// and leaves the idle-push gate unarmed.
    /// </summary>
    [Fact]
    public async Task HandleSessionCreatedAsync_Should_RejectThePlaceholderServerUrl_When_ServerBoundIsFalse()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var payload = Payload(SessionId);
        payload.ServerUrl = "http://localhost:4096/";
        payload.ServerBound = false;

        // act
        await _handler.HandleSessionCreatedAsync(payload, dryRun: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.Equal(AgentSessionEndpointKind.None, row.EndpointKind);
        Assert.Equal(string.Empty, row.EndpointAddr);
        Assert.Null(row.EndpointSecret);

        // assert
        Assert.False(await _sessions.ClaimIdlePushAsync(CurrentGeneration(), cancellationToken));

        // assert
        Assert.True(await _sessions.IsAnnouncementPendingAsync(CurrentGeneration(), cancellationToken));
    }

    /// <summary>
    /// A session demoted to <c>endpoint_kind = 'none'</c> at session.created must stay unarmed for the
    /// idle-push gate even after a genuine chat message.
    /// </summary>
    [Fact]
    public async Task HandleChatMessageAsync_Should_NotArmIdlePush_When_EndpointIsUntrusted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var untrustedPayload = Payload(SessionId);
        untrustedPayload.ServerUrl = "http://localhost:4096/";
        untrustedPayload.ServerBound = false;
        await _handler.HandleSessionCreatedAsync(untrustedPayload, dryRun: true, cancellationToken);

        // act
        await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.False(await _sessions.ClaimIdlePushAsync(CurrentGeneration(), cancellationToken));
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
    public async Task HandleSessionIdleAsync_Should_NotSpendTheIdlePushArmedFlag_When_ItArrivesBeforeTheDaemon()
    {
        // arrange
        // Session creation arms the idle-push gate before the idle event.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);

        // act
        // Handle the idle event before claiming the push gate.
        var outcome = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, outcome);
        Assert.True(await _sessions.ClaimIdlePushAsync(CurrentGeneration(), cancellationToken));
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
    public async Task HandleSessionIdleAsync_Should_SkipTheHeartbeatTouch_When_HooksAreSuppressed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await StartAndGetActorAsync(cancellationToken);
        var beforeSuppressed = (await FindRowAsync(cancellationToken))!.LastBeatAt;
        _environmentVariables.Set("NITRO_HOOK_SUPPRESS", "1");
        _timeProvider.Advance(TimeSpan.FromMinutes(1));

        // act
        var suppressed = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, suppressed);
        Assert.Equal(beforeSuppressed, (await FindRowAsync(cancellationToken))!.LastBeatAt);

        // act
        _environmentVariables.Set("NITRO_HOOK_SUPPRESS", "0");
        var resumed = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, resumed);
        Assert.Equal(_timeProvider.GetUtcNow(), (await FindRowAsync(cancellationToken))!.LastBeatAt);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_RemainNeutral_When_TheSessionIsDeletedBeforeReservation()
    {
        // arrange
        // Claim the announcement before injecting deletion during digest reservation.
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
    public async Task HandleChatMessageAsync_Should_LeaveTheAnnouncementArmed_When_DigestReservationFails()
    {
        // arrange
        // Inject a reservation failure before the pending announcement is claimed.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        var handler = CreateHandler(new ThrowingDeliveryLedger());

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken));

        // assert
        Assert.True(await _sessions.IsAnnouncementPendingAsync(CurrentGeneration(), cancellationToken));

        // assert
        var retry = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.Contains("Your Nitro actor name is", retry.Parts[0]);
        var again = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.Equal(OpencodeHookOutcome.Neutral, again);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_RestoreTheDigestReservation_When_ClaimAnnouncementAsyncFails()
    {
        // arrange
        // Inject an announcement-claim failure after the digest reservation commits.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        var handler = CreateHandler(new ThrowingAnnouncementSessionRegistry(_sessions));

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken));

        // assert
        var retry = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.Equal(2, retry.Parts.Count);
        Assert.Contains("Your Nitro actor name is", retry.Parts[0]);
        Assert.Contains("1 unread nitro message.", retry.Parts[1]);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_ReleaseOnlyThisTurnsReservations_When_ClaimAnnouncementAsyncFails()
    {
        // arrange
        // Reserve c before the failing turn; a and b remain available to that turn.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var a = await SendMailAsync("bob", actor, cancellationToken);
        var b = await SendMailAsync("bob", actor, cancellationToken);
        var c = await SendMailAsync("bob", actor, cancellationToken);
        await _ledger.ReserveAsync(
            CurrentGeneration(), [c.Id], AgentSessionChannel.Digest, _timeProvider.GetUtcNow(), cancellationToken);
        var handler = CreateHandler(new ThrowingAnnouncementSessionRegistry(_sessions));

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken));

        // assert
        // This turn's reservations can be acquired again.
        var reReservedAb = await _ledger.ReserveAsync(
            CurrentGeneration(),
            [a.Id, b.Id],
            AgentSessionChannel.Digest,
            _timeProvider.GetUtcNow(),
            cancellationToken);
        Assert.Equal(2, reReservedAb.Count);

        // assert
        // The earlier reservation remains held.
        var reReservedC = await _ledger.ReserveAsync(
            CurrentGeneration(), [c.Id], AgentSessionChannel.Digest, _timeProvider.GetUtcNow(), cancellationToken);
        Assert.Empty(reReservedC);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_StillReleaseTheDigestReservation_When_TheTurnsTokenIsAlreadyCancelled()
    {
        // arrange
        // The injected claim cancels the turn token before throwing.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var handler = CreateHandler(new CancellingAnnouncementSessionRegistry(_sessions, cts));

        // act
        await Assert.ThrowsAnyAsync<Exception>(
            () => handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cts.Token));

        // assert
        // Retry with an uncancelled token.
        var retry = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.Equal(2, retry.Parts.Count);
        Assert.Contains("Your Nitro actor name is", retry.Parts[0]);
        Assert.Contains("1 unread nitro message.", retry.Parts[1]);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_PropagateTheOriginalException_When_TheCompensatingReleaseThrows()
    {
        // arrange
        // Inject distinct exceptions for the announcement claim and compensating release.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        var handler = CreateHandler(
            new ThrowingAnnouncementSessionRegistry(_sessions), new ReleaseThrowingDeliveryLedger(_ledger));

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken));

        // assert
        Assert.Equal("Simulated announcement-claim failure.", exception.Message);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_AnnounceOnce_When_ThePreviousAppendWasReportedUndelivered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await StartAndGetActorAsync(cancellationToken);
        await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var undeliveredPayload = Payload(SessionId);
        undeliveredPayload.Delivered = false;

        // act
        // Report the previous announcement as undelivered.
        var undelivered = await _handler.HandleChatMessageAsync(undeliveredPayload, dryRun: true, cancellationToken);

        // assert
        Assert.Contains("Your Nitro actor name is", Assert.Single(undelivered.Parts));

        // act
        // Confirm delivery on the following turn.
        var confirmedPayload = Payload(SessionId);
        confirmedPayload.Delivered = true;
        var again = await _handler.HandleChatMessageAsync(confirmedPayload, dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, again);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_AnnounceOnce_When_UndeliveredArrivesOnANitroPushedPayload()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await StartAndGetActorAsync(cancellationToken);
        await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var pushedUndeliveredPayload = Payload(SessionId);
        pushedUndeliveredPayload.NitroPushed = true;
        pushedUndeliveredPayload.Delivered = false;

        // act
        // Report failed delivery on a Nitro-pushed turn.
        var pushed = await _handler.HandleChatMessageAsync(pushedUndeliveredPayload, dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, pushed);

        // act
        // Submit a genuine chat message after the Nitro-pushed turn.
        var next = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Contains("Your Nitro actor name is", Assert.Single(next.Parts));
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_NotAnnounceAgain_When_ADeliveredAnnouncementIsFollowedByANeutralTurn()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await StartAndGetActorAsync(cancellationToken);
        await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var deliveredPayload = Payload(SessionId);
        deliveredPayload.Delivered = true;

        // act
        // Confirm delivery without adding new mail.
        var neutral = await _handler.HandleChatMessageAsync(deliveredPayload, dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, neutral);

        // act
        var stillDeliveredPayload = Payload(SessionId);
        stillDeliveredPayload.Delivered = true;
        var again = await _handler.HandleChatMessageAsync(stillDeliveredPayload, dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, again);
        Assert.False(await _sessions.IsAnnouncementPendingAsync(CurrentGeneration(), cancellationToken));
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_ReleaseTheDigestReservation_When_ThePreviousAppendWasReportedUndelivered()
    {
        // arrange
        // Obtain the first response before reporting its append as undelivered.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        var first = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var undeliveredPayload = Payload(SessionId);
        undeliveredPayload.Delivered = false;

        // act
        var undelivered = await _handler.HandleChatMessageAsync(undeliveredPayload, dryRun: true, cancellationToken);

        // assert
        Assert.Equal(2, first.Parts.Count);
        Assert.Equal(2, undelivered.Parts.Count);
        Assert.Contains("Your Nitro actor name is", undelivered.Parts[0]);
        Assert.Contains("1 unread nitro message.", undelivered.Parts[1]);
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
        HarnessVersion = "1.18.25",
        ServerBound = true
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

    private OpencodeHookHandler CreateHandler(IAgentSessionRegistry sessionRegistry) => new(
        _fileSystem,
        _timeProvider,
        sessionRegistry,
        _ledger,
        _mail,
        _environmentVariables,
        new FixedInstanceIdProvider("host-1"),
        new FixedGlobalConfigDirectoryProvider(_workspaceRoot));

    private OpencodeHookHandler CreateHandler(
        IAgentSessionRegistry sessionRegistry, ISessionDeliveryLedger ledger) => new(
        _fileSystem,
        _timeProvider,
        sessionRegistry,
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
