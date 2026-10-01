using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Tests <see cref="CodexHookHandler"/> session lifecycle and mail notifications
/// against a real workspace database, capturing queue attempts with a fake client.
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
    private readonly AgentStore _agentStore;
    private readonly AgentDeliveryLedger _ledger;
    private readonly MailStore _mail;
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
        _agentStore = new AgentStore(_fileSystem, _timeProvider, _database);
        _ledger = new AgentDeliveryLedger(_fileSystem, _database);
        _mail = new MailStore(_fileSystem, _timeProvider, _database, _agentStore);
        _queueClient = new FakeCodexQueueClient();

        _handler = CreateHandler();
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    private CodexHookHandler CreateHandler(ICodexHarnessVersionResolver? harnessVersionResolver = null) => new(
        _fileSystem,
        _timeProvider,
        _agentStore,
        _ledger,
        _mail,
        harnessVersionResolver ?? new FixedCodexHarnessVersionResolver(),
        _queueClient);

    // ---------- SessionStart ----------

    [Fact]
    public async Task HandleSessionStartAsync_Should_MintAnAgentAndAnnounceTheNameOnly_When_TheSessionIsUnknown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), cancellationToken);

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
        var first = await _handler.HandleSessionStartAsync(Payload(SessionId), cancellationToken);

        // act
        var second = await _handler.HandleSessionStartAsync(Payload(SessionId), cancellationToken);

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
        await _handler.HandleSessionStartAsync(Payload(SessionId), cancellationToken);
        var actor = (await FindRowAsync(cancellationToken))!.Name;
        await _agentStore.SetRoleAsync(actor, "researcher", cancellationToken);
        await _handler.HandleSessionEndAsync(Payload(SessionId), cancellationToken);

        // act
        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), cancellationToken);

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
        var outcome = await _handler.HandleSessionStartAsync(Payload(SessionId), cancellationToken);

        // assert
        Assert.Equal(CodexHookOutcome.Neutral, outcome);
        var after = await FindRowAsync(cancellationToken);
        Assert.Equal(before!.LastSeenAt, after!.LastSeenAt);
        Assert.Empty(await _ledger.FindDeliveredAsync(actor, [message.Id], cancellationToken));
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_SetTheCodexThreadEndpointToTheSessionId_When_TheSessionIdIsAValidAddress()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        await _handler.HandleSessionStartAsync(Payload(SessionId), cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.Equal(AgentSessionEndpointKind.CodexThread, row.EndpointKind);
        Assert.Equal(SessionId, row.EndpointAddr);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_ReturnNeutralWithoutCreatingARow_When_CwdHasNoWorkspace()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var noWorkspaceRoot = Directory.CreateTempSubdirectory("nitro-codex-hook-no-workspace-tests");

        try
        {
            var payload = new CodexHookPayload { SessionId = SessionId, Cwd = noWorkspaceRoot.FullName };

            // act
            var outcome = await _handler.HandleSessionStartAsync(payload, cancellationToken);

            // assert
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
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var payload = new CodexHookPayload { SessionId = SessionId, Cwd = null };

        // act
        var outcome = await _handler.HandleSessionStartAsync(payload, cancellationToken);

        // assert
        Assert.Equal(CodexHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_NotCreateAProvisionalIdentity_When_SessionIdIsMissing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var payload = new CodexHookPayload { SessionId = null, Cwd = _workspaceRoot };

        // act
        var first = await _handler.HandleSessionStartAsync(payload, cancellationToken);
        var second = await _handler.HandleSessionStartAsync(payload, cancellationToken);

        // assert
        Assert.Equal(CodexHookOutcome.Neutral, first);
        Assert.Equal(CodexHookOutcome.Neutral, second);
        Assert.Equal(0L, await CountAllAgentRowsAsync(cancellationToken));
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_RecordHarnessVersion_When_TheResolverReturnsOne()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var handler = CreateHandler(new FixedCodexHarnessVersionResolver("0.101.0"));

        // act
        await handler.HandleSessionStartAsync(Payload(SessionId), cancellationToken);

        // assert
        var row = await FindRowAsync(cancellationToken);
        Assert.Equal("0.101.0", row!.HarnessVersion);
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_LeaveHarnessVersionBlank_When_TheResolverReturnsNone()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        await _handler.HandleSessionStartAsync(Payload(SessionId), cancellationToken);

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
        await _handler.HandleSessionStartAsync(Payload(SessionId), cancellationToken);
        var before = (await FindRowAsync(cancellationToken))!.LastSeenAt;
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), cancellationToken);

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
        var outcome = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), cancellationToken);

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
        var outcome = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), cancellationToken);

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
        await _handler.HandleSessionStartAsync(Payload(SessionId), cancellationToken);

        // act
        var outcome = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), cancellationToken);

        // assert
        Assert.Equal(CodexHookOutcome.Neutral, outcome);
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
        var outcome = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), cancellationToken);

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
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_ReturnNeutral_When_CalledAgainWithNoNewMail()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        var first = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), cancellationToken);
        Assert.NotNull(first.AdditionalContext);

        // act
        var second = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), cancellationToken);

        // assert
        Assert.Equal(CodexHookOutcome.Neutral, second);
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
        var outcome = await _handler.HandleUserPromptSubmitAsync(Payload(SessionId), cancellationToken);

        // assert
        Assert.Equal(CodexHookOutcome.Neutral, outcome);
        var after = await FindRowAsync(cancellationToken);
        Assert.Equal(before!.LastSeenAt, after!.LastSeenAt);
        Assert.Empty(await _ledger.FindDeliveredAsync(actor, [message.Id], cancellationToken));
    }

    // ---------- SessionEnd ----------

    [Fact]
    public async Task HandleSessionEndAsync_Should_StampEndedAtAndKeepTheRow_When_TheSessionIsActive()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), cancellationToken);

        // act
        var outcome = await _handler.HandleSessionEndAsync(Payload(SessionId), cancellationToken);

        // assert
        Assert.Equal(CodexHookOutcome.Neutral, outcome);
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
        var outcome = await _handler.HandleSessionEndAsync(Payload(SessionId), cancellationToken);

        // assert
        Assert.Equal(CodexHookOutcome.Neutral, outcome);
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
        var outcome = await _handler.HandleSessionEndAsync(Payload(SessionId), cancellationToken);

        // assert
        Assert.Equal(CodexHookOutcome.Neutral, outcome);
        var row = await FindRowAsync(cancellationToken);
        Assert.Null(row!.EndedAt);
    }

    // ---------- Notify (the idle-turn gate) ----------

    [Fact]
    public async Task HandleNotifyAsync_Should_AdvanceLastSeenAt_When_TheThreadResolves()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), cancellationToken);
        var before = (await FindRowAsync(cancellationToken))!.LastSeenAt;
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        await _handler.HandleNotifyAsync(NotifyPayload(SessionId), cancellationToken);

        // assert
        var after = (await FindRowAsync(cancellationToken))!.LastSeenAt;
        Assert.True(after > before);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_ReturnNeutral_When_TypeIsNotAgentTurnComplete()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);

        // act
        var outcome = await _handler.HandleNotifyAsync(
            NotifyPayload(SessionId, type: "something-else"), cancellationToken);

        // assert
        Assert.Equal(CodexNotifyOutcome.Neutral, outcome);
        Assert.Empty(_queueClient.Calls);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_ReturnNeutral_When_NoMailIsAddressedToTheGeneratedActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _handler.HandleSessionStartAsync(Payload(SessionId), cancellationToken);

        // act
        var outcome = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), cancellationToken);

        // assert
        Assert.Equal(CodexNotifyOutcome.Neutral, outcome);
        Assert.Empty(_queueClient.Calls);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_QueueTheDigestJson_When_UnreadMailExistsForTheClaimedActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var message = await SendMailAsync("bob", actor, cancellationToken);

        // act
        var outcome = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), cancellationToken);

        // assert
        Assert.True(outcome.Queued);
        var call = Assert.Single(_queueClient.Calls);
        Assert.Equal(SessionId, call.ThreadId);
        Assert.Contains("1 shown below as `nitro agent mail read --thread --output json` prints them.", call.Message);
        Assert.Contains(message.Id, call.Message);
        Assert.Contains("\"items\"", call.Message);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_LeaveTheMessageUnread_When_ItQueuesTheDigest()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var message = await SendMailAsync("bob", actor, cancellationToken);
        var unreadBefore = await _mail.CountUnreadAsync(actor, cancellationToken);

        // act
        var outcome = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), cancellationToken);

        // assert
        Assert.True(outcome.Queued);
        var call = Assert.Single(_queueClient.Calls);
        Assert.Contains("\"read\": false", call.Message);
        var unread = await _mail.QueryInboxAsync(
            new MailInboxFilter { Actor = actor, UnreadOnly = true }, cancellationToken);
        Assert.Contains(unread, m => m.Id == message.Id);
        Assert.Equal(unreadBefore, await _mail.CountUnreadAsync(actor, cancellationToken));
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_QueueTheInboxPointer_When_MailWasSeenOnThePingChannel()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var message = await SendMailAsync("bob", actor, cancellationToken);
        await _ledger.ReserveAsync(
            actor, [message.Id], AgentSessionChannel.Ping, _timeProvider.GetUtcNow(), cancellationToken);

        // act
        var outcome = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), cancellationToken);

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
        // arrange
        // Call notify twice for the same thread without sending new mail.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        var first = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), cancellationToken);
        Assert.True(first.Queued);

        // act
        var second = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), cancellationToken);

        // assert
        Assert.Equal(CodexNotifyOutcome.Neutral, second);
        Assert.Single(_queueClient.Calls);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_QueueAgain_When_NewMailArrivesAfterAnEarlierQueue()
    {
        // arrange
        // A new message remains eligible after an earlier message was queued.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        await _handler.HandleNotifyAsync(NotifyPayload(SessionId), cancellationToken);
        await SendMailAsync("carol", actor, cancellationToken);

        // act
        var outcome = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), cancellationToken);

        // assert
        Assert.True(outcome.Queued);
        Assert.Equal(2, _queueClient.Calls.Count);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_ReturnNeutral_When_QueueClientFails()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);
        _queueClient.NextResult = CodexQueueResult.Error;
        var outcome = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), cancellationToken);
        Assert.False(outcome.Queued);

        // act
        // Retry the same message after the failed queue attempt.
        var retried = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), cancellationToken);

        // assert
        Assert.Equal(CodexNotifyOutcome.Neutral, retried);
        Assert.Single(_queueClient.Calls);
    }

    [Fact]
    public async Task HandleNotifyAsync_Should_ReturnNeutralWithoutWriting_When_TheSessionBelongsToADeletedAgent()
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
        var outcome = await _handler.HandleNotifyAsync(NotifyPayload(SessionId), cancellationToken);

        // assert
        Assert.Equal(CodexNotifyOutcome.Neutral, outcome);
        var after = await FindRowAsync(cancellationToken);
        Assert.Equal(before!.LastSeenAt, after!.LastSeenAt);
        Assert.Empty(_queueClient.Calls);
        Assert.Empty(await _ledger.FindDeliveredAsync(actor, [message.Id], cancellationToken));
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
    {
        // Registers the mail sender behind the store's sender-usability check.
        await SeedAgentAsync(sender, cancellationToken);

        return await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = sender, Subject = "status", Body = "please check", To = [recipient] },
            cancellationToken);
    }

    private async Task<string> StartAndGetActorAsync(CancellationToken cancellationToken)
    {
        await _handler.HandleSessionStartAsync(Payload(SessionId), cancellationToken);
        var row = await FindRowAsync(cancellationToken);

        return row!.Name;
    }

    private Task<AgentRow?> FindRowAsync(CancellationToken cancellationToken)
        => _agentStore.FindBySessionAsync(AgentSessionHarness.Codex, SessionId, cancellationToken);

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
