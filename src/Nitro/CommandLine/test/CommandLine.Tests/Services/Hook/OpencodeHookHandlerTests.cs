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
    public async Task HandleSessionIdleAsync_Should_NotSpendTheIdlePushArmedFlag_When_ItArrivesBeforeTheDaemon()
    {
        // arrange: HandleSessionCreatedAsync already armed the idle-push
        // gate; ActorWakeDispatcher (not this hook) is its sole claimant.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);

        // act: the opencode-generated idle event reaches this hook before
        // ActorWakeDispatcher's own poll does.
        var outcome = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert: a neutral response (the shim discards it either way), and
        // the one-shot claim is still there for the dispatcher to spend.
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

        // assert: the suppressed call never reaches the heartbeat touch.
        Assert.Equal(OpencodeHookOutcome.Neutral, suppressed);
        Assert.Equal(beforeSuppressed, (await FindRowAsync(cancellationToken))!.LastBeatAt);

        // act: unsuppressed, the same event does touch the heartbeat.
        _environmentVariables.Set("NITRO_HOOK_SUPPRESS", "0");
        var resumed = await _handler.HandleSessionIdleAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(OpencodeHookOutcome.Neutral, resumed);
        Assert.Equal(_timeProvider.GetUtcNow(), (await FindRowAsync(cancellationToken))!.LastBeatAt);
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
    public async Task HandleChatMessageAsync_Should_LeaveTheAnnouncementArmed_When_DigestReservationFails()
    {
        // arrange: the digest step runs BEFORE the announcement is claimed,
        // so it is made to throw here, standing in for the mail-store or
        // ledger failures OpencodeHookExecutor's fail-open catch turns into
        // a neutral response with nothing delivered.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        var handler = CreateHandler(new ThrowingDeliveryLedger());

        // act: the digest step throws before the claim is even attempted,
        // so nothing has committed yet and the marker stays exactly as
        // armed as it was on entry - no compensation needed.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken));

        // assert: read directly off the registry row, not merely inferred
        // from what the next message carries.
        Assert.True(await _sessions.IsAnnouncementPendingAsync(CurrentGeneration(), cancellationToken));

        // assert: the next message still announces, exactly once (the still
        // unread mail rides along in the same digest the failed attempt
        // never got to reserve).
        var retry = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.Contains("Your Nitro actor name is", retry.Parts[0]);
        var again = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.Equal(OpencodeHookOutcome.Neutral, again);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_RestoreTheDigestReservation_When_ClaimAnnouncementAsyncFails()
    {
        // arrange: the digest reservation commits successfully (there is
        // real unread mail), then the registry write that is supposed to
        // follow it - ClaimAnnouncementAsync - throws. Without compensation
        // the already-committed reservation stays spent for a response this
        // turn never returns.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        var handler = CreateHandler(new ThrowingAnnouncementSessionRegistry(_sessions));

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken));

        // assert: the reservation was released, so the retry still delivers
        // the same unread message's digest alongside the still-armed
        // announcement, instead of the mail having been silently burned.
        var retry = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.Equal(2, retry.Parts.Count);
        Assert.Contains("Your Nitro actor name is", retry.Parts[0]);
        Assert.Contains("1 unread nitro message.", retry.Parts[1]);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_AnnounceOnce_When_ThePreviousAppendWasReportedUndelivered()
    {
        // arrange: the first chat message shows the announcement and, per
        // this handler's contract, claims the marker optimistically on
        // emission - the shim has no way to confirm delivery until the next
        // turn.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await StartAndGetActorAsync(cancellationToken);
        await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var undeliveredPayload = Payload(SessionId);
        undeliveredPayload.Delivered = false;

        // act: the shim now reports that the first announcement never
        // landed. This turn re-arms the marker and, being the very next
        // call, immediately reclaims and re-shows it in this same response.
        var undelivered = await _handler.HandleChatMessageAsync(undeliveredPayload, dryRun: true, cancellationToken);

        // assert: the reporting turn announces exactly once.
        Assert.Contains("Your Nitro actor name is", Assert.Single(undelivered.Parts));

        // act: the message after that, once the retry is confirmed
        // delivered.
        var confirmedPayload = Payload(SessionId);
        confirmedPayload.Delivered = true;
        var again = await _handler.HandleChatMessageAsync(confirmedPayload, dryRun: true, cancellationToken);

        // assert: the confirming message stays neutral rather than
        // repeating it.
        Assert.Equal(OpencodeHookOutcome.Neutral, again);
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_AnnounceOnce_When_UndeliveredArrivesOnANitroPushedPayload()
    {
        // arrange: the first chat message shows and claims the announcement.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await StartAndGetActorAsync(cancellationToken);
        await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var pushedUndeliveredPayload = Payload(SessionId);
        pushedUndeliveredPayload.NitroPushed = true;
        pushedUndeliveredPayload.Delivered = false;

        // act: a Nitro-pushed turn reports the earlier genuine turn's
        // announcement never landed. The re-arm runs above the NitroPushed
        // early return, so the marker survives this otherwise-neutral turn
        // instead of being dropped with it.
        var pushed = await _handler.HandleChatMessageAsync(pushedUndeliveredPayload, dryRun: true, cancellationToken);

        // assert: the pushed turn itself stays neutral.
        Assert.Equal(OpencodeHookOutcome.Neutral, pushed);

        // act: the next genuine chat message.
        var next = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert: it announces exactly once.
        Assert.Contains("Your Nitro actor name is", Assert.Single(next.Parts));
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_NotAnnounceAgain_When_ADeliveredAnnouncementIsFollowedByANeutralTurn()
    {
        // arrange: the first chat message shows and claims the announcement.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await StartAndGetActorAsync(cancellationToken);
        await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var deliveredPayload = Payload(SessionId);
        deliveredPayload.Delivered = true;

        // act: the next turn confirms delivery but, like an ordinary steady
        // -state turn with nothing new to say, carries nothing to append.
        var neutral = await _handler.HandleChatMessageAsync(deliveredPayload, dryRun: true, cancellationToken);

        // assert: that turn stays neutral rather than re-announcing.
        Assert.Equal(OpencodeHookOutcome.Neutral, neutral);

        // act: a further turn, still reporting delivery.
        var stillDeliveredPayload = Payload(SessionId);
        stillDeliveredPayload.Delivered = true;
        var again = await _handler.HandleChatMessageAsync(stillDeliveredPayload, dryRun: true, cancellationToken);

        // assert: it also stays neutral, and the registry row confirms the
        // marker was never re-armed by the intervening neutral turn.
        Assert.Equal(OpencodeHookOutcome.Neutral, again);
        Assert.False(await _sessions.IsAnnouncementPendingAsync(CurrentGeneration(), cancellationToken));
    }

    [Fact]
    public async Task HandleChatMessageAsync_Should_ReleaseTheDigestReservation_When_ThePreviousAppendWasReportedUndelivered()
    {
        // arrange: the first chat message reserves and delivers the unread
        // mail digest alongside the announcement.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        var first = await _handler.HandleChatMessageAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var undeliveredPayload = Payload(SessionId);
        undeliveredPayload.Delivered = false;

        // act: the shim reports the response carrying that digest (and the
        // announcement alongside it) never landed. Without releasing the
        // still-committed reservation, this same call's own digest build
        // would find the message already reserved and stay silent forever;
        // releasing it first frees it to be reserved and delivered again in
        // this very call, alongside the re-armed-then-reclaimed
        // announcement.
        var undelivered = await _handler.HandleChatMessageAsync(undeliveredPayload, dryRun: true, cancellationToken);

        // assert: the still-unread message is reserved and delivered again,
        // proving the earlier reservation was released rather than lost,
        // and the announcement rides along since it never landed either.
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

    private OpencodeHookHandler CreateHandler(IAgentSessionRegistry sessionRegistry) => new(
        _fileSystem,
        _timeProvider,
        sessionRegistry,
        _ledger,
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

    public Task ReleaseAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        string channel,
        CancellationToken cancellationToken)
        => inner.ReleaseAsync(generation, messageIds, channel, cancellationToken);
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
