using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Agents;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

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
    private readonly AgentStore _agentStore;
    private readonly AgentDeliveryLedger _ledger;
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
        _agentStore = new AgentStore(_fileSystem, _timeProvider, _database);
        _ledger = new AgentDeliveryLedger(_fileSystem, _database);
        _mail = new MailStore(_fileSystem, _timeProvider, _database, _agentStore);

        _handler = CreateHandler();
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    private ClaudeHookHandler CreateHandler(IClaudeSessionFileReader? sessionFileReader = null) => new(
        _fileSystem,
        _timeProvider,
        _agentStore,
        _ledger,
        _mail,
        sessionFileReader ?? new FixedClaudeSessionFileReader());

    // ---------- SessionStart ----------

    [Fact]
    public async Task HandleSessionStartAsync_Should_MintAnAgentAndAnnounceTheNameOnly_When_TheSessionIsUnknown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.Equal("", row.Role);
        outcome.AdditionalContext!.Replace(row.Name, "<actor>").MatchInlineSnapshot(
            """
            Your Nitro actor name is "<actor>". Pass this name to the `--actor` option to act under this actor explicitly.
            """);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_ReuseTheSameAgent_When_CalledAgainForTheSameSession()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var first = await _handler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // act
        var second = await _handler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(first.AdditionalContext, second.AdditionalContext);
        var row = await FindRowAsync(cancellationToken);
        Assert.Contains($"\"{row!.Name}\"", second.AdditionalContext);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_ReuseAnEndedRowAndAnnounceNameAndRole_When_TheSessionResumes()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);
        var actor = (await FindRowAsync(cancellationToken))!.Name;
        await _agentStore.SetRoleAsync(actor, "researcher", cancellationToken);
        await _handler.HandleSessionEndAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // act
        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.Equal(actor, row!.Name);
        Assert.Null(row.EndedAt);
        outcome.AdditionalContext!.Replace(actor, "<actor>").MatchInlineSnapshot(
            """
            Your Nitro actor name is "<actor>". Pass this name to the `--actor` option to act under this actor explicitly.
            Your Nitro role is "researcher".
            """);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_ReturnNeutralWithoutWriting_When_TheSessionBelongsToADeletedAgent()
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
        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
        var after = await FindRowAsync(cancellationToken);
        Assert.Equal(before!.LastSeenAt, after!.LastSeenAt);
        Assert.Empty(await _ledger.FindDeliveredAsync(actor, [message.Id], cancellationToken));
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
            var outcome = await _handler.HandleSessionStartAsync(payload, skipSessionFileLookup: true, cancellationToken);

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
        var outcome = await _handler.HandleSessionStartAsync(payload, skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_NotCreateAProvisionalIdentity_When_SessionIdIsMissing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var payload = new ClaudeHookPayload { SessionId = null, Cwd = _workspaceRoot };

        // act
        var first = await _handler.HandleSessionStartAsync(payload, skipSessionFileLookup: true, cancellationToken);
        var second = await _handler.HandleSessionStartAsync(payload, skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, first);
        Assert.Equal(ClaudeHookOutcome.Neutral, second);
        Assert.Equal(0L, await CountAllAgentRowsAsync(cancellationToken));
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_RecordHarnessVersion_When_TheSessionFileCarriesOne()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var handler = CreateHandler(new FixedClaudeSessionFileReader(new ClaudeSessionFile(SessionId, "", "", "2.1.241")));

        // act
        // skipSessionFileLookup: false enables the session-file lookup.
        await handler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: false, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.Equal("2.1.241", row!.HarnessVersion);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_LeaveHarnessVersionBlank_When_SessionFileLookupIsSkipped()
    {
        // arrange
        // Skipping the session-file lookup omits session metadata.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        await _handler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.Equal("", row!.HarnessVersion);
    }

    // ---------- UserPromptSubmit ----------

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_AdvanceLastSeenAt_When_TheSessionResolves()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);
        var before = (await FindRowAsync(cancellationToken))!.LastSeenAt;
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        var after = (await FindRowAsync(cancellationToken))!.LastSeenAt;
        Assert.True(after > before);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_AnnounceTheName_When_TheSessionIsUnknown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var outcome = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        outcome.AdditionalContext!.Replace(row.Name, "<actor>").MatchInlineSnapshot(
            """
            Your Nitro actor name is "<actor>". Pass this name to the `--actor` option to act under this actor explicitly.
            """);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_AnnounceTheNameThenTheDigest_When_TheSessionIsUnknownAndMailIsPending()
    {
        // arrange
        // A free pool name is reserved for mail, then freed so the mint below has to draw it.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var freeName = AgentActorAllocator.BaseActors[0];
        await SeedAgentAsync(freeName, cancellationToken);
        var message = await SendMailAsync("bob", freeName, cancellationToken);
        await DeleteAgentRowAsync(freeName, cancellationToken);
        await TombstoneOtherPoolNamesAsync(freeName, cancellationToken);

        // act
        var outcome = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.Equal(freeName, row!.Name);
        outcome.AdditionalContext!.Replace(message.Id, "<message-id>").MatchInlineSnapshot(
            $$"""
            Your Nitro actor name is "{{freeName}}". Pass this name to the `--actor` option to act under this actor explicitly.

            You have 1 unread nitro message; 1 shown below as `nitro agent mail read --thread --output json` prints them. Reply with `nitro agent mail reply --message <id> --actor {{freeName}} --body "..."` or ack with `nitro agent mail ack --message <id> --actor {{freeName}}`; anything not shown is in `nitro agent mail inbox --unread --actor {{freeName}}`.
            {
              "items": [
                {
                  "id": "<message-id>",
                  "threadId": "<message-id>",
                  "inReplyTo": null,
                  "from": "bob",
                  "to": [
                    "{{freeName}}"
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
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ReturnNeutral_When_NoMailIsAddressedToTheActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // act
        var outcome = await _handler.HandleUserPromptSubmitAsync(
            Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

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
            Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

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
        var outcome = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

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
        var first = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);
        Assert.NotNull(first.AdditionalContext);

        // act
        var second = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

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
        var first = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);
        Assert.NotNull(first.AdditionalContext);
        await _mail.MarkUnreadAsync([message.Id], actor, cancellationToken);

        // act
        var second = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, second);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ResetTheBlockBudget_When_ItIsCalled()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);

        for (var i = 0; i < ClaudeHookHandler.MaxBlocksPerTurn; i++)
        {
            await SendMailAsync($"bob-{i}", actor, cancellationToken);
            await _handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);
        }

        var exhaustedRow = await FindRowAsync(cancellationToken);
        Assert.Equal(ClaudeHookHandler.MaxBlocksPerTurn, exhaustedRow!.BlockBudgetUsed);

        // act
        await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        var resetRow = await FindRowAsync(cancellationToken);
        Assert.Equal(0, resetRow!.BlockBudgetUsed);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ReturnNeutralWithoutWriting_When_TheSessionBelongsToADeletedAgent()
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
        var outcome = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
        var after = await FindRowAsync(cancellationToken);
        Assert.Equal(before!.LastSeenAt, after!.LastSeenAt);
        Assert.Empty(await _ledger.FindDeliveredAsync(actor, [message.Id], cancellationToken));
    }

    // ---------- Stop ----------

    [Fact]
    public async Task HandleStopAsync_Should_AdvanceLastSeenAt_When_TheSessionResolves()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);
        var before = (await FindRowAsync(cancellationToken))!.LastSeenAt;
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        await _handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        var after = (await FindRowAsync(cancellationToken))!.LastSeenAt;
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
            Payload(SessionId, stopHookActive: true), skipSessionFileLookup: true, cancellationToken);

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
        var outcome = await _handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

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
        var outcome = await _handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

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

        var first = await _handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);
        Assert.True(first.Block);
        Assert.Contains(firstMessage.Id, first.BlockReason);

        var pingSeenMessage = await SendMailAsync("carol", actor, cancellationToken);
        await _ledger.ReserveAsync(
            actor, [pingSeenMessage.Id], AgentSessionChannel.Ping, _timeProvider.GetUtcNow(), cancellationToken);

        // act
        var outcome = await _handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

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
        var first = await _handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);
        Assert.True(first.Block);

        // act
        var second = await _handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

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
            var outcome = await _handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);
            Assert.True(outcome.Block);
        }

        await SendMailAsync("bob-over-budget", actor, cancellationToken);

        // act
        var overBudget = await _handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

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
            await _handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);
        }

        var pending = await SendMailAsync("bob-pending", actor, cancellationToken);
        await _handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken); // over budget, no-op

        // act
        await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);
        var afterReset = await _handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.True(afterReset.Block);
        Assert.NotNull(pending.Id);
    }

    [Fact]
    public async Task HandleStopAsync_Should_ReturnNeutral_When_TheBudgetIncrementNoLongerMatchesTheRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);

        var handler = new ClaudeHookHandler(
            _fileSystem,
            _timeProvider,
            new IncrementNeverMatchesAgentStore(_agentStore),
            _ledger,
            _mail,
            new FixedClaudeSessionFileReader());

        // act
        var outcome = await handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

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

        var spyLedger = new ReserveCapturingAgentDeliveryLedger(_ledger);
        var handler = new ClaudeHookHandler(
            _fileSystem,
            _timeProvider,
            _agentStore,
            spyLedger,
            _mail,
            new FixedClaudeSessionFileReader());

        // act
        var outcome = await handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.True(outcome.Block);
        Assert.NotNull(spyLedger.LastMessageIds);
        Assert.True(spyLedger.LastMessageIds!.Count <= MailDigestPolicy.MaxMessages);
    }

    [Fact]
    public async Task HandleStopAsync_Should_ResolveTheSameAgent_When_ReplayedFromADifferentHandlerInstance()
    {
        // arrange
        // Both handlers resolve the same harness session id against the same database.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var sessionStartHandler = CreateHandler();
        await sessionStartHandler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);
        var actor = (await FindRowAsync(cancellationToken))!.Name;
        await SendMailAsync("bob", actor, cancellationToken);

        // act
        var stopHandler = CreateHandler();
        var outcome = await stopHandler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.True(outcome.Block);
        Assert.NotNull(outcome.BlockReason);
    }

    [Fact]
    public async Task HandleStopAsync_Should_ReturnNeutralWithoutWriting_When_TheSessionBelongsToADeletedAgent()
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
        var outcome = await _handler.HandleStopAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
        var after = await FindRowAsync(cancellationToken);
        Assert.Equal(before!.LastSeenAt, after!.LastSeenAt);
        Assert.Empty(await _ledger.FindDeliveredAsync(actor, [message.Id], cancellationToken));
    }

    // ---------- Notification ----------

    [Fact]
    public async Task HandleNotificationAsync_Should_ReturnNeutralWithoutTouching_When_TheNotificationTypeIsNotIdlePrompt()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);
        var before = (await FindRowAsync(cancellationToken))!.LastSeenAt;
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var outcome = await _handler.HandleNotificationAsync(
            Payload(SessionId, notificationType: "permission_request"), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
        var after = (await FindRowAsync(cancellationToken))!.LastSeenAt;
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task HandleNotificationAsync_Should_AdvanceLastSeenAt_When_TheNotificationTypeIsIdlePromptAndTheSessionResolves()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);
        var before = (await FindRowAsync(cancellationToken))!.LastSeenAt;
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var outcome = await _handler.HandleNotificationAsync(
            Payload(SessionId, notificationType: "idle_prompt"), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
        var after = (await FindRowAsync(cancellationToken))!.LastSeenAt;
        Assert.True(after > before);
    }

    [Fact]
    public async Task HandleNotificationAsync_Should_AnnounceTheName_When_TheSessionIsUnknownAndTheNotificationTypeIsIdlePrompt()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var outcome = await _handler.HandleNotificationAsync(
            Payload(SessionId, notificationType: "idle_prompt"), skipSessionFileLookup: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        outcome.AdditionalContext!.Replace(row.Name, "<actor>").MatchInlineSnapshot(
            """
            Your Nitro actor name is "<actor>". Pass this name to the `--actor` option to act under this actor explicitly.
            """);
    }

    [Fact]
    public async Task HandleNotificationAsync_Should_ReturnNeutralWithoutWriting_When_TheSessionBelongsToADeletedAgent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await MarkDeletedAsync(actor, cancellationToken);
        var before = await FindRowAsync(cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var outcome = await _handler.HandleNotificationAsync(
            Payload(SessionId, notificationType: "idle_prompt"), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
        var after = await FindRowAsync(cancellationToken);
        Assert.Equal(before!.LastSeenAt, after!.LastSeenAt);
    }

    // ---------- SessionEnd ----------

    [Fact]
    public async Task HandleSessionEndAsync_Should_StampEndedAtAndKeepTheRow_When_TheSessionIsActive()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // act
        var outcome = await _handler.HandleSessionEndAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.NotNull(row.EndedAt);
    }

    [Fact]
    public async Task HandleSessionEndAsync_Should_ReturnNeutral_When_NoRowExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var outcome = await _handler.HandleSessionEndAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleSessionEndAsync_Should_ReturnNeutralWithoutClearingDeletedAt_When_TheSessionBelongsToADeletedAgent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await MarkDeletedAsync(actor, cancellationToken);

        // act
        var outcome = await _handler.HandleSessionEndAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
        var row = await FindRowAsync(cancellationToken);
        Assert.Null(row!.EndedAt);
    }

    // ---------- Subagent sessions ----------

    [Fact]
    public async Task HandleSessionStartAsync_Should_ReturnNeutralWithoutMintingOrAnnouncing_When_AgentIdIsSet()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var outcome = await _handler.HandleSessionStartAsync(
            Payload(SessionId, agentId: "agent-1"), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
        Assert.Null(await FindRowAsync(cancellationToken));
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_MintAndAnnounce_When_OnlyAgentTypeIsSet()
    {
        // arrange
        // agent_type alone (a top-level `claude --agent <name>` session) is not a subagent marker.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var json = $$"""{"session_id":"{{SessionId}}","cwd":{{JsonSerializer.Serialize(_workspaceRoot)}},"agent_type":"general-purpose"}""";
        var payload = JsonSerializer.Deserialize(json, ClaudeHookJsonContext.Default.ClaudeHookPayload)!;

        // act
        var outcome = await _handler.HandleSessionStartAsync(payload, skipSessionFileLookup: true, cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        outcome.AdditionalContext!.Replace(row.Name, "<actor>").MatchInlineSnapshot(
            """
            Your Nitro actor name is "<actor>". Pass this name to the `--actor` option to act under this actor explicitly.
            """);
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ReturnNeutralWithoutMintingOrDigest_When_AgentIdIsSet()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var message = await SendMailAsync("bob", actor, cancellationToken);
        var before = await FindRowAsync(cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var outcome = await _handler.HandleUserPromptSubmitAsync(
            Payload(SessionId, agentId: "agent-1"), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
        var after = await FindRowAsync(cancellationToken);
        Assert.Equal(before!.LastSeenAt, after!.LastSeenAt);
        Assert.Empty(await _ledger.FindDeliveredAsync(actor, [message.Id], cancellationToken));
    }

    [Fact]
    public async Task HandleStopAsync_Should_ReturnNeutralWithoutBlockingOrDigest_When_AgentIdIsSet()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var message = await SendMailAsync("bob", actor, cancellationToken);
        var before = await FindRowAsync(cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var outcome = await _handler.HandleStopAsync(
            Payload(SessionId, agentId: "agent-1"), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.False(outcome.Block);
        var after = await FindRowAsync(cancellationToken);
        Assert.Equal(before!.LastSeenAt, after!.LastSeenAt);
        Assert.Equal(before.BlockBudgetUsed, after!.BlockBudgetUsed);
        Assert.Empty(await _ledger.FindDeliveredAsync(actor, [message.Id], cancellationToken));
    }

    [Fact]
    public async Task HandleNotificationAsync_Should_ReturnNeutralWithoutMinting_When_AgentIdIsSetAndNotificationTypeIsIdlePrompt()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var outcome = await _handler.HandleNotificationAsync(
            Payload(SessionId, notificationType: "idle_prompt", agentId: "agent-1"),
            skipSessionFileLookup: true,
            cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
        Assert.Null(await FindRowAsync(cancellationToken));
    }

    [Fact]
    public async Task HandleSessionEndAsync_Should_ReturnNeutralWithoutStampingEndedAt_When_AgentIdIsSet()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await StartAndGetActorAsync(cancellationToken);

        // act
        var outcome = await _handler.HandleSessionEndAsync(
            Payload(SessionId, agentId: "agent-1"), skipSessionFileLookup: true, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
        Assert.Null((await FindRowAsync(cancellationToken))!.EndedAt);
    }

    // ---------- helpers ----------

    private ClaudeHookPayload Payload(
        string sessionId,
        bool stopHookActive = false,
        string? notificationType = null,
        string? agentId = null) => new()
        {
            SessionId = sessionId,
            Cwd = _workspaceRoot,
            StopHookActive = stopHookActive,
            NotificationType = notificationType,
            AgentId = agentId
        };

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }

    private async Task<MailMessage> SendMailAsync(string sender, string recipient, CancellationToken cancellationToken)
    {
        // Registers the mail sender behind the store's sender-usability check.
        await SeedAgentAsync(sender, cancellationToken);

        return await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = sender, Subject = "status", Body = "please check", To = [recipient] },
            cancellationToken);
    }

    private async Task<string> StartAndGetActorAsync(CancellationToken cancellationToken)
    {
        await _handler.HandleSessionStartAsync(Payload(SessionId), skipSessionFileLookup: true, cancellationToken);
        var row = await FindRowAsync(cancellationToken);

        return row!.Name;
    }

    private async Task AssertStillUnreadAsync(
        string actor, string messageId, int unreadBefore, CancellationToken cancellationToken)
    {
        var unread = await _mail.QueryInboxAsync(
            new MailInboxFilter { Actor = actor, UnreadOnly = true }, cancellationToken);
        Assert.Contains(unread, message => message.Id == messageId);
        Assert.Equal(unreadBefore, await _mail.CountUnreadAsync(actor, cancellationToken));
    }

    private Task<AgentRow?> FindRowAsync(CancellationToken cancellationToken)
        => _agentStore.FindBySessionAsync(AgentSessionHarness.ClaudeCode, SessionId, cancellationToken);

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

    /// <summary>
    /// Hard-deletes the named agent's row, freeing it for the actor allocator to draw again.
    /// </summary>
    private async Task DeleteAgentRowAsync(string name, CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM agents WHERE name = @name";
        command.Parameters.AddWithValue("@name", name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Tombstones every pool name except <paramref name="keep"/>, forcing the actor
    /// allocator to draw that one name for the next mint.
    /// </summary>
    private async Task TombstoneOtherPoolNamesAsync(string keep, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO agents (name, registered_at, started_at, last_seen_at, deleted_at) "
            + "VALUES (@name, @now, @now, @now, @now)";
        var nameParameter = command.Parameters.Add("@name", SqliteType.Text);
        command.Parameters.AddWithValue("@now", now);

        foreach (var name in AgentActorAllocator.BaseActors.Where(name => name != keep))
        {
            nameParameter.Value = name;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
