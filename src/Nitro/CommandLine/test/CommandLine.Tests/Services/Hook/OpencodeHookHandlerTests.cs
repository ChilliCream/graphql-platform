using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
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
    private readonly AgentStore _agentStore;
    private readonly AgentDeliveryLedger _ledger;
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
        _agentStore = new AgentStore(_fileSystem, _timeProvider, _database);
        _ledger = new AgentDeliveryLedger(_fileSystem, _database);
        _mail = new MailStore(_fileSystem, _timeProvider, _database, _agentStore);
        _environmentVariables = new FixedEnvironmentVariableProvider();
        _handler = CreateHandler();
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    private OpencodeHookHandler CreateHandler() => new(
        _fileSystem, _timeProvider, _agentStore, _ledger, _mail, _environmentVariables);

    private OpencodeHookHandler CreateHandler(IAgentDeliveryLedger ledger) => new(
        _fileSystem, _timeProvider, _agentStore, ledger, _mail, _environmentVariables);

    private OpencodeHookHandler CreateHandler(IAgentStore agentStore) => new(
        _fileSystem, _timeProvider, agentStore, _ledger, _mail, _environmentVariables);

    private OpencodeHookHandler CreateHandler(IAgentStore agentStore, IAgentDeliveryLedger ledger) => new(
        _fileSystem, _timeProvider, agentStore, ledger, _mail, _environmentVariables);

    // ---------- SessionCreated ----------

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
    public async Task HandleSessionCreatedAsync_Should_ReuseTheSameAgent_When_CalledAgainForTheSameSession()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionCreatedAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var actor = (await FindRowAsync(cancellationToken))!.Name;

        // act
        await _handler.HandleSessionCreatedAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.Equal(actor, row!.Name);
        Assert.Equal(1L, await CountAllAgentRowsAsync(cancellationToken));
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
        Assert.False(await _agentStore.ClaimIdlePushAsync(row.Name, cancellationToken));
        Assert.True(await _agentStore.IsAnnouncementPendingAsync(row.Name, cancellationToken));
    }

    [Fact]
    public async Task HandleSessionCreatedAsync_Should_ReturnNeutralWithoutWriting_When_TheSessionBelongsToADeletedAgent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var message = await SendMailAsync("bob", actor, cancellationToken);
        await MarkDeletedAsync(actor, cancellationToken);
        var before = await FindRowAsync(cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var outcome = await _handler.HandleSessionCreatedAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, outcome);
        var after = await FindRowAsync(cancellationToken);
        Assert.Equal(before!.LastSeenAt, after!.LastSeenAt);
        Assert.Empty(await _ledger.FindDeliveredAsync(actor, [message.Id], cancellationToken));
    }

    [Fact]
    public async Task HandleSessionCreatedAsync_Should_ReturnNeutralWithoutCreatingARow_When_TheServerUrlIsMissing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var payload = Payload(SessionId);
        payload.ServerUrl = null;

        // act
        var outcome = await _handler.HandleSessionCreatedAsync(payload, dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, outcome);
        Assert.Equal(0L, await CountAllAgentRowsAsync(cancellationToken));
    }

    // ---------- ChatMessage ----------

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
        var actor = (await FindRowAsync(cancellationToken))!.Name;

        // act
        await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.False(await _agentStore.ClaimIdlePushAsync(actor, cancellationToken));
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
    public async Task HandleChatMessageAsync_Should_ResetThePerTurnBudget_When_AGenuineChatMessageArrives()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionCreatedAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var actor = (await FindRowAsync(cancellationToken))!.Name;
        await _agentStore.IncrementBlockBudgetAsync(actor, cancellationToken);

        // act
        await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.Equal(0, row!.BlockBudgetUsed);
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
        await _agentStore.IncrementBlockBudgetAsync(actor, cancellationToken);
        var pushedPayload = Payload(SessionId);
        pushedPayload.NitroPushed = true;

        // act
        var pushed = await _handler.HandleChatMessageAsync(pushedPayload, dryRun: true, cancellationToken);
        await SendMailAsync("carol", actor, cancellationToken);
        var idle = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, pushed);
        Assert.Equal(OpencodeHookOutcome.Neutral, idle);
        Assert.Equal(1, (await FindRowAsync(cancellationToken))!.BlockBudgetUsed);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_ReturnNeutralWithoutWriting_When_TheSessionBelongsToADeletedAgent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var message = await SendMailAsync("bob", actor, cancellationToken);
        await MarkDeletedAsync(actor, cancellationToken);
        var before = await FindRowAsync(cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var outcome = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, outcome);
        var after = await FindRowAsync(cancellationToken);
        Assert.Equal(before!.LastSeenAt, after!.LastSeenAt);
        Assert.Empty(await _ledger.FindDeliveredAsync(actor, [message.Id], cancellationToken));
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_ReturnNeutralWithoutMinting_When_TheSessionIsUnknown()
    {
        // arrange
        // Today's OpenCode behavior, unchanged: chat-message never mints a fresh agent.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var outcome = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, outcome);
        Assert.Null(await FindRowAsync(cancellationToken));
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
        Assert.True(await _agentStore.ClaimIdlePushAsync(actor, cancellationToken));
    }

    [Fact]
    public async Task HandleSessionIdleAsync_Should_SkipTheHeartbeatTouch_When_HooksAreSuppressed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await StartAndGetActorAsync(cancellationToken);
        var beforeSuppressed = (await FindRowAsync(cancellationToken))!.LastSeenAt;
        _environmentVariables.Set("NITRO_HOOK_SUPPRESS", "1");
        _timeProvider.Advance(TimeSpan.FromMinutes(1));

        // act
        var suppressed = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, suppressed);
        Assert.Equal(beforeSuppressed, (await FindRowAsync(cancellationToken))!.LastSeenAt);

        // act
        _environmentVariables.Set("NITRO_HOOK_SUPPRESS", "0");
        var resumed = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, resumed);
        Assert.Equal(_timeProvider.GetUtcNow(), (await FindRowAsync(cancellationToken))!.LastSeenAt);
    }

    [Fact]
    public async Task HandleSessionIdleAsync_Should_ReturnNeutralWithoutWriting_When_TheSessionBelongsToADeletedAgent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await MarkDeletedAsync(actor, cancellationToken);
        var before = await FindRowAsync(cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var outcome = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, outcome);
        var after = await FindRowAsync(cancellationToken);
        Assert.Equal(before!.LastSeenAt, after!.LastSeenAt);
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
        Assert.True(await _agentStore.IsAnnouncementPendingAsync(actor, cancellationToken));

        // act
        var retry = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Contains("Your Nitro actor name is", retry.Parts[0]);

        // act
        var again = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
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
        var handler = CreateHandler(new ThrowingAnnouncementAgentStore(_agentStore));

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
            actor, [c.Id], AgentSessionChannel.Digest, _timeProvider.GetUtcNow(), cancellationToken);
        var handler = CreateHandler(new ThrowingAnnouncementAgentStore(_agentStore));

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken));

        // assert
        // This turn's reservations can be acquired again.
        var reReservedAb = await _ledger.ReserveAsync(
            actor, [a.Id, b.Id], AgentSessionChannel.Digest, _timeProvider.GetUtcNow(), cancellationToken);
        Assert.Equal(2, reReservedAb.Count);

        // assert
        // The earlier reservation remains held.
        var reReservedC = await _ledger.ReserveAsync(
            actor, [c.Id], AgentSessionChannel.Digest, _timeProvider.GetUtcNow(), cancellationToken);
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
        var handler = CreateHandler(new CancellingAnnouncementAgentStore(_agentStore, cts));

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
            new ThrowingAnnouncementAgentStore(_agentStore), new ReleaseThrowingDeliveryLedger(_ledger));

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
        var actor = await StartAndGetActorAsync(cancellationToken);
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
        Assert.False(await _agentStore.IsAnnouncementPendingAsync(actor, cancellationToken));
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
    public async Task HandleChatMessageAsync_Should_AnnounceAgain_When_TheSessionIsCreatedAfterEnding()
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

    // ---------- SessionDeleted ----------

    [Fact]
    public async Task HandleSessionDeletedAsync_Should_StampEndedAtAndKeepTheRow_When_TheSessionIsActive()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionCreatedAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // act
        var outcome = await _handler.HandleSessionDeletedAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, outcome);
        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.NotNull(row.EndedAt);
    }

    [Fact]
    public async Task HandleSessionDeletedAsync_Should_ReturnNeutral_When_NoRowExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var outcome = await _handler.HandleSessionDeletedAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleSessionDeletedAsync_Should_ReturnNeutralWithoutClearingDeletedAt_When_TheSessionBelongsToADeletedAgent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await MarkDeletedAsync(actor, cancellationToken);

        // act
        var outcome = await _handler.HandleSessionDeletedAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, outcome);
        var row = await FindRowAsync(cancellationToken);
        Assert.Null(row!.EndedAt);
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

    // ---------- helpers ----------

    private OpencodeHookPayload Payload(string sessionId) => new()
    {
        SessionId = sessionId,
        Cwd = _workspaceRoot,
        ServerUrl = "http://127.0.0.1:4096",
        ServerPassword = "secret",
        HarnessVersion = "1.18.25",
        ServerBound = true
    };

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

        return row!.Name;
    }

    private async Task<MailMessage> SendMailAsync(string sender, string recipient, CancellationToken cancellationToken)
    {
        // Registers the mail sender behind the store's sender-usability check.
        await SeedAgentAsync(sender, cancellationToken);

        return await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = sender, Subject = "status", Body = "please check", To = [recipient] },
            cancellationToken);
    }

    private Task<AgentRow?> FindRowAsync(CancellationToken cancellationToken)
        => _agentStore.FindBySessionAsync(AgentSessionHarness.Opencode, SessionId, cancellationToken);

    private async Task<long> CountAllAgentRowsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM agents;";

        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    /// <summary>
    /// Registers the named agent directly against the unified <c>agents</c> table.
    /// </summary>
    private async Task SeedAgentAsync(string name, CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agents (name, registered_at, started_at, last_seen_at)
            VALUES (@name, @now, @now, @now)
            ON CONFLICT (name) DO UPDATE SET last_seen_at = excluded.last_seen_at;
            """;
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@now", _timeProvider.GetUtcNow());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Marks the named agent as soft-deleted.
    /// </summary>
    private async Task MarkDeletedAsync(string name, CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE agents SET deleted_at = @now WHERE name = @name";
        command.Parameters.AddWithValue("@now", _timeProvider.GetUtcNow());
        command.Parameters.AddWithValue("@name", name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
