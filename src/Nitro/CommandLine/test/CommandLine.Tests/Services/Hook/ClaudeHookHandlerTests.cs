using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using ChilliCream.Nitro.CommandLine.Tests.Agents;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Tests <see cref="ClaudeHookHandler"/> session lifecycle, mail notifications,
/// and per-turn blocking limits against a real workspace database.
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
            new FixedGlobalConfigDirectoryProvider(_workspaceRoot));
        _ledger = new SessionDeliveryLedger(_fileSystem, _database);
        _mail = new MailStore(_fileSystem, _timeProvider, _database, _agentRegistry);

        _handler = CreateHandler();
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    private ClaudeHookHandler CreateHandler() => CreateHandler(_agentRegistry);

    private ClaudeHookHandler CreateHandler(IAgentRegistry agentRegistry) => new(
        _fileSystem,
        _timeProvider,
        _sessions,
        agentRegistry,
        _ledger,
        _mail,
        new FixedClaudeSessionFileReader(),
        new FixedInstanceIdProvider("host-1"),
        new FixedGlobalConfigDirectoryProvider(_workspaceRoot));

    // ---------- SessionStart ----------

    [Fact]
    public async Task HandleSessionStartAsync_Should_BindTheRowToAGeneratedActor_When_NoEnvActorIsSet()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
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
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var first = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // act
        var second = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.Equal(first.AdditionalContext, second.AdditionalContext);
        Assert.Contains($"Your Nitro actor name is \"{row.AgentName}\".", second.AdditionalContext);
        Assert.Equal(AgentSessionBindingKind.Explicit, row.BindingKind);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_UseDurableAgentRole_When_SessionRoleIsEmpty()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await _sessions.RegisterAsync(
            CurrentGeneration(), actor: null, actorGiven: false, role: "researcher", roleGiven: true,
            cancellationToken: cancellationToken);
        await _sessions.SetRoleAsync(CurrentGeneration(), string.Empty, cancellationToken);

        // act
        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        outcome.AdditionalContext!.Replace(actor, "<actor>").MatchInlineSnapshot(
            """
            Your Nitro actor name is "<actor>". Pass this name to the `--actor` option to act under this actor explicitly.
            Your Nitro role is "researcher".
            """);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_UseSessionRole_When_DurableAgentRoleAlsoExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await _sessions.RegisterAsync(
            CurrentGeneration(), actor: null, actorGiven: false, role: "researcher", roleGiven: true,
            cancellationToken: cancellationToken);
        await _sessions.SetRoleAsync(CurrentGeneration(), "planner", cancellationToken);

        // act
        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        outcome.AdditionalContext!.Replace(actor, "<actor>").MatchInlineSnapshot(
            """
            Your Nitro actor name is "<actor>". Pass this name to the `--actor` option to act under this actor explicitly.
            Your Nitro role is "planner".
            """);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_OmitRole_When_SessionRoleIsEmptyAndAgentIsMissing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var agentRegistry = new Mock<IAgentRegistry>(MockBehavior.Strict);
        agentRegistry
            .Setup(registry => registry.GetAsync(It.IsAny<string>(), cancellationToken))
            .ReturnsAsync((AgentRecord?)null);
        var handler = CreateHandler(agentRegistry.Object);

        // act
        var outcome = await handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var actor = (await FindRowAsync(cancellationToken))!.AgentName!;

        // assert
        outcome.AdditionalContext!.Replace(actor, "<actor>").MatchInlineSnapshot(
            """
            Your Nitro actor name is "<actor>". Pass this name to the `--actor` option to act under this actor explicitly.
            """);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_PreserveActorContext_When_DurableRoleLookupFails()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var agentRegistry = new Mock<IAgentRegistry>(MockBehavior.Strict);
        agentRegistry
            .Setup(registry => registry.GetAsync(It.IsAny<string>(), cancellationToken))
            .ThrowsAsync(new InvalidOperationException("lookup failed"));
        var handler = CreateHandler(agentRegistry.Object);

        // act
        var outcome = await handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var actor = (await FindRowAsync(cancellationToken))!.AgentName!;

        // assert
        outcome.AdditionalContext!.Replace(actor, "<actor>").MatchInlineSnapshot(
            """
            Your Nitro actor name is "<actor>". Pass this name to the `--actor` option to act under this actor explicitly.
            """);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_ReturnNeutralWithoutCreatingARow_When_CwdHasNoWorkspace()
    {
        // arrange
        // The payload uses a separate temporary root with no ancestor workspace.
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
    public async Task HandleSessionStartAsync_Should_ReturnNeutral_When_CwdIsMissing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var payload = new ClaudeHookPayload { SessionId = SessionId, Cwd = null };

        // act
        var outcome = await _handler.HandleSessionStartAsync(payload, dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_NotCreateAProvisionalIdentity_When_SessionIdIsMissing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var payload = new ClaudeHookPayload { SessionId = null, Cwd = _workspaceRoot };

        // act
        var first = await _handler.HandleSessionStartAsync(payload, dryRun: true, cancellationToken);
        var second = await _handler.HandleSessionStartAsync(payload, dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, first);
        Assert.Equal(ClaudeHookOutcome.Neutral, second);
        Assert.Equal(0L, await CountAllSessionRowsAsync(cancellationToken));
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_RecordHarnessVersion_When_TheSessionFileCarriesOne()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var handler = new ClaudeHookHandler(
            _fileSystem,
            _timeProvider,
            _sessions,
            _agentRegistry,
            _ledger,
            _mail,
            new FixedClaudeSessionFileReader(new ClaudeSessionFile(SessionId, "", "", "2.1.241")),
            new FixedInstanceIdProvider("host-1"),
            new FixedGlobalConfigDirectoryProvider(_workspaceRoot));

        // act
        // dryRun: false enables the session-file lookup.
        await handler.HandleSessionStartAsync(Payload(SessionId), dryRun: false, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.Equal("2.1.241", row!.HarnessVersion);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_LeaveHarnessVersionBlank_When_TheResolverReturnsNone()
    {
        // arrange
        // Dry-run resolution omits session-file metadata.
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
    public async Task HandleUserPromptSubmitAsync_Should_ReturnNeutral_When_NoMailIsAddressedToTheActor()
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
        var actor = await StartAndGetActorAsync(cancellationToken);
        var message = await SendMailAsync("bob", actor, cancellationToken);

        // act
        var outcome = await _handler.HandleUserPromptSubmitAsync(
            Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        outcome.AdditionalContext!
            .Replace(actor, "<actor>")
            .Replace(message.Id, "<message-id>")
            .MatchInlineSnapshot(
                """
                You have 1 unread nitro message; 1 shown below as `nitro agent mail read --thread --output json` prints them. Reply with `nitro agent mail reply --message <id> --actor <actor> --body "..."` or ack with `nitro agent mail ack --message <id> --actor <actor>`; anything not shown is in `nitro agent mail inbox --unread --actor <actor>`.
                {
                  "items": [
                    {
                      "id": "<message-id>",
                      "threadId": "<message-id>",
                      "inReplyTo": null,
                      "from": "bob",
                      "to": [
                        "<actor>"
                      ],
                      "cc": [],
                      "subject": "status",
                      "body": "please check",
                      "createdAt": "2026-01-10T12:00:00+00:00",
                      "read": false,
                      "archived": false,
                      "takeovers": []
                    }
                  ]
                }
                """);
        Assert.False(outcome.Block);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_LeaveTheMessageUnread_When_ItReturnsTheDigest()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var message = await SendMailAsync("bob", actor, cancellationToken);
        var unreadBefore = await _mail.CountUnreadAsync(actor, cancellationToken);

        // act
        var outcome = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Contains("\"read\": false", outcome.AdditionalContext);
        await AssertStillUnreadAsync(actor, message.Id, unreadBefore, cancellationToken);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ReturnNeutral_When_CalledAgainWithNoNewMail()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
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
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var message = await SendMailAsync("bob", actor, cancellationToken);
        var first = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.NotNull(first.AdditionalContext);
        await _mail.MarkUnreadAsync([message.Id], actor, cancellationToken);

        // act
        var second = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, second);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ResetTheBlockBudget()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);

        for (var i = 0; i < ClaudeHookHandler.MaxBlocksPerTurn; i++)
        {
            await SendMailAsync($"bob-{i}", actor, cancellationToken);
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
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);

        // act
        var outcome = await _handler.HandleStopAsync(
            Payload(SessionId, stopHookActive: true), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleStopAsync_Should_IncludeTheDigest_When_UnreadMailHasNotBeenDelivered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var message = await SendMailAsync("bob", actor, cancellationToken);

        // act
        var outcome = await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.True(outcome.Block);
        Assert.StartsWith(
            "Unread nitro mail is waiting; handle it before ending this turn, or ignore this once if it is not actionable right now.\n",
            outcome.BlockReason);
        Assert.Contains(message.Id, outcome.BlockReason);
        Assert.Contains("\"items\"", outcome.BlockReason);
    }

    [Fact]
    public async Task HandleStopAsync_Should_LeaveTheMessageUnread_When_ItGatesOnUnreadMail()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var message = await SendMailAsync("bob", actor, cancellationToken);
        var unreadBefore = await _mail.CountUnreadAsync(actor, cancellationToken);

        // act
        var outcome = await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.True(outcome.Block);
        Assert.Contains("\"read\": false", outcome.BlockReason);
        await AssertStillUnreadAsync(actor, message.Id, unreadBefore, cancellationToken);
    }

    [Fact]
    public async Task HandleStopAsync_Should_KeepTheBlockReason_When_AFreshStopDigestIsFollowedByPingSeenMail()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var firstMessage = await SendMailAsync("bob", actor, cancellationToken);

        var first = await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);
        Assert.True(first.Block);
        Assert.Contains(firstMessage.Id, first.BlockReason);

        var pingSeenMessage = await SendMailAsync("carol", actor, cancellationToken);
        var generation = CurrentGeneration();
        await _ledger.ReserveAsync(
            generation.Harness,
            generation.SessionId,
            [pingSeenMessage.Id],
            AgentSessionChannel.Ping,
            _timeProvider.GetUtcNow(),
            cancellationToken);

        // act
        var outcome = await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.True(outcome.Block);
        Assert.Equal(
            $"Unread nitro mail is waiting. Read it with `nitro agent mail inbox --actor {actor}` "
                + "before ending this turn, or ignore this once if it is not actionable right now.",
            outcome.BlockReason);
    }

    [Fact]
    public async Task HandleStopAsync_Should_ReturnNeutral_When_CalledAgainForTheSameUnreadMail()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
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
        // arrange
        // Each iteration supplies mail with no previous gate reservation.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);

        for (var i = 0; i < ClaudeHookHandler.MaxBlocksPerTurn; i++)
        {
            await SendMailAsync($"bob-{i}", actor, cancellationToken);
            var outcome = await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);
            Assert.True(outcome.Block);
        }

        await SendMailAsync("bob-over-budget", actor, cancellationToken);

        // act
        var overBudget = await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, overBudget);
    }

    [Fact]
    public async Task HandleStopAsync_Should_LeaveTheMessageEligibleForAFutureBudgetCycle_When_OverBudget()
    {
        // arrange
        // Exhaust the budget before sending the pending message.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);

        for (var i = 0; i < ClaudeHookHandler.MaxBlocksPerTurn; i++)
        {
            await SendMailAsync($"bob-{i}", actor, cancellationToken);
            await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);
        }

        var pending = await SendMailAsync("bob-pending", actor, cancellationToken);
        await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken); // over budget, no-op

        // act
        await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var afterReset = await _handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.True(afterReset.Block);
        Assert.NotNull(pending.Id);
    }

    [Fact]
    public async Task HandleStopAsync_Should_ReturnNeutral_When_TheRowIsDeletedBetweenResolveAndIncrement()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);

        var handler = new ClaudeHookHandler(
            _fileSystem,
            _timeProvider,
            new IncrementNeverMatchesAgentSessionRegistry(_sessions),
            _agentRegistry,
            _ledger,
            _mail,
            new FixedClaudeSessionFileReader(),
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
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);

        for (var i = 0; i < MailDigestPolicy.MaxMessages + 5; i++)
        {
            await SendMailAsync($"bob-{i}", actor, cancellationToken);
        }

        var spyLedger = new ReserveCapturingSessionDeliveryLedger(_ledger);
        var handler = new ClaudeHookHandler(
            _fileSystem,
            _timeProvider,
            _sessions,
            _agentRegistry,
            spyLedger,
            _mail,
            new FixedClaudeSessionFileReader(),
            new FixedInstanceIdProvider("host-1"),
            new FixedGlobalConfigDirectoryProvider(_workspaceRoot));

        // act
        var outcome = await handler.HandleStopAsync(Payload(SessionId), dryRun: true, cancellationToken);

        // assert
        Assert.True(outcome.Block);
        Assert.NotNull(spyLedger.LastMessageIds);
        Assert.True(spyLedger.LastMessageIds!.Count <= MailDigestPolicy.MaxMessages);
    }

    [Fact]
    public async Task HandleStopAsync_Should_ResolveTheSameGeneration_When_ReplayedFromADifferentHandlerInstance()
    {
        // arrange
        // Both handlers use the same session id and fixed Nitro instance id.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var sessionStartHandler = CreateHandler();
        await sessionStartHandler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var actor = (await FindRowAsync(cancellationToken))!.AgentName!;
        await SendMailAsync("bob", actor, cancellationToken);

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
        // arrange
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

    private async Task<string> StartAndGetActorAsync(CancellationToken cancellationToken)
    {
        await _handler.HandleSessionStartAsync(Payload(SessionId), dryRun: true, cancellationToken);
        var row = await FindRowAsync(cancellationToken);

        return row!.AgentName!;
    }

    private async Task AssertStillUnreadAsync(
        string actor, string messageId, int unreadBefore, CancellationToken cancellationToken)
    {
        var unread = await _mail.QueryInboxAsync(
            new MailInboxFilter { Actor = actor, UnreadOnly = true }, cancellationToken);
        Assert.Contains(unread, message => message.Id == messageId);
        Assert.Equal(unreadBefore, await _mail.CountUnreadAsync(actor, cancellationToken));
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

    private static AgentSessionGeneration CurrentGeneration()
        => new(AgentSessionHarness.ClaudeCode, SessionId, "host-1");
}
