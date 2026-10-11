using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Agents;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Tests <see cref="ClaudeHookHandler"/> bound to the Venti harness and Venti's peer registry,
/// against a real workspace database and a temporary registry directory.
/// </summary>
public sealed class VentiHookHandlerTests : IDisposable
{
    private const string SessionId = "0123456789abcdef0123456789abcdef";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceRoot;
    private readonly string _workspaceDirectory;
    private readonly string _peerDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentDatabase _database;
    private readonly AgentStore _agentStore;
    private readonly AgentDeliveryLedger _ledger;
    private readonly MailStore _mail;
    private readonly ClaudeHookHandler _venti;
    private readonly ClaudeHookHandler _claude;

    public VentiHookHandlerTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-venti-hook-handler-tests");
        _workspaceRoot = Path.Combine(_tempRoot.FullName, "repo");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_workspaceRoot);
        Directory.CreateDirectory(_workspaceDirectory);
        _peerDirectory = Path.Combine(_tempRoot.FullName, "home", ".venti", "peers");
        Directory.CreateDirectory(_peerDirectory);
        _fileSystem = new TestFileSystem(_workspaceRoot);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        _database = new AgentDatabase();
        _agentStore = new AgentStore(_fileSystem, _timeProvider, _database);
        _ledger = new AgentDeliveryLedger(_fileSystem, _database);
        _mail = new MailStore(_fileSystem, _timeProvider, _database, _agentStore);
        _venti = CreateHandler(AgentSessionHarness.Venti, new VentiPeerRegistryReader(_peerDirectory));
        _claude = CreateHandler(AgentSessionHarness.ClaudeCode, new FixedClaudeSessionFileReader());
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task HandleSessionStartAsync_Should_BindSeparateRows_When_ClaudeAndVentiReportTheSameSessionId()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _claude.HandleSessionStartAsync(Payload(), skipSessionFileLookup: false, cancellationToken);

        // act
        await _venti.HandleSessionStartAsync(Payload(), skipSessionFileLookup: false, cancellationToken);

        // assert
        var claudeRow = await _agentStore.FindBySessionAsync(
            AgentSessionHarness.ClaudeCode, SessionId, cancellationToken);
        var ventiRow = await _agentStore.FindBySessionAsync(AgentSessionHarness.Venti, SessionId, cancellationToken);
        Assert.NotEqual(claudeRow!.Name, ventiRow!.Name);
        new[] { claudeRow, ventiRow }
            .Select(row => new { row.Harness, Display = AgentHarnessDisplay.Name(row.Harness) })
            .MatchInlineSnapshot(
                """
                [
                  {
                    "Harness": "claude-code",
                    "Display": "Claude Code"
                  },
                  {
                    "Harness": "venti",
                    "Display": "Venti"
                  }
                ]
                """);
    }

    [Fact]
    public async Task HandleSessionEndAsync_Should_EndOnlyTheVentiRow_When_AClaudeSessionSharesTheSessionId()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _claude.HandleSessionStartAsync(Payload(), skipSessionFileLookup: false, cancellationToken);
        await _venti.HandleSessionStartAsync(Payload(), skipSessionFileLookup: false, cancellationToken);

        // act
        await _venti.HandleSessionEndAsync(Payload(), skipSessionFileLookup: false, cancellationToken);

        // assert
        var claudeRow = await _agentStore.FindBySessionAsync(
            AgentSessionHarness.ClaudeCode, SessionId, cancellationToken);
        var ventiRow = await _agentStore.FindBySessionAsync(AgentSessionHarness.Venti, SessionId, cancellationToken);
        Assert.Equal(
            (AgentState.Unreachable, AgentState.Offline),
            (AgentStateResolver.Resolve(claudeRow!, _timeProvider.GetUtcNow()),
                AgentStateResolver.Resolve(ventiRow!, _timeProvider.GetUtcNow())));
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_AdvertiseThePeerEndpoint_When_AnInteractiveSessionIsRegistered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await WriteRegistryAsync(kind: "interactive", protocol: 1, cancellationToken);

        // act
        await _venti.HandleSessionStartAsync(Payload(), skipSessionFileLookup: false, cancellationToken);

        // assert
        var row = await _agentStore.FindBySessionAsync(AgentSessionHarness.Venti, SessionId, cancellationToken);
        Assert.Equal(
            (AgentSessionEndpointKind.ClaudePeer, SessionId, AgentState.Online),
            (row!.EndpointKind, row.EndpointAddr, AgentStateResolver.Resolve(row, _timeProvider.GetUtcNow())));
    }

    [Fact]
    public async Task HandleSessionStartAsync_Should_LeaveTheAgentUnreachable_When_TheSessionAdvertisesNoEndpoint()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        await _venti.HandleSessionStartAsync(Payload(), skipSessionFileLookup: false, cancellationToken);

        // assert
        var row = await _agentStore.FindBySessionAsync(AgentSessionHarness.Venti, SessionId, cancellationToken);
        Assert.Equal(
            (AgentSessionEndpointKind.None, AgentState.Unreachable),
            (row!.EndpointKind, AgentStateResolver.Resolve(row, _timeProvider.GetUtcNow())));
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_AdoptThePeerEndpoint_When_TheRegistryAppearsAfterSessionStart()
    {
        // arrange
        // Venti starts SessionStart hooks before it opens the session's peer endpoint.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _venti.HandleSessionStartAsync(Payload(), skipSessionFileLookup: false, cancellationToken);
        await WriteRegistryAsync(kind: "interactive", protocol: 1, cancellationToken);

        // act
        await _venti.HandleUserPromptSubmitAsync(Payload(), skipSessionFileLookup: false, cancellationToken);

        // assert
        var row = await _agentStore.FindBySessionAsync(AgentSessionHarness.Venti, SessionId, cancellationToken);
        Assert.Equal((AgentSessionEndpointKind.ClaudePeer, SessionId), (row!.EndpointKind, row.EndpointAddr));
    }

    [Fact]
    public async Task HandleUserPromptSubmitAsync_Should_InjectTheDigest_When_TheVentiPayloadHasNullTranscriptPath()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var message = await SendMailAsync("bob", actor, cancellationToken);
        var payload = JsonSerializer.Deserialize(
            $$"""
            {
              "session_id": "{{SessionId}}",
              "transcript_path": null,
              "cwd": {{JsonSerializer.Serialize(_workspaceRoot)}},
              "hook_event_name": "UserPromptSubmit",
              "permission_mode": "default",
              "prompt": "continue"
            }
            """,
            ClaudeHookJsonContext.Default.ClaudeHookPayload)!;

        // act
        var outcome = await _venti.HandleUserPromptSubmitAsync(
            payload, skipSessionFileLookup: false, cancellationToken);

        // assert
        outcome.AdditionalContext!.Replace(actor, "<actor>").Replace(message.Id, "<message-id>").MatchInlineSnapshot(
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
    public async Task HandleStopAsync_Should_BlockWithTheDigest_When_UnreadMailWaits()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        var message = await SendMailAsync("bob", actor, cancellationToken);

        // act
        var outcome = await _venti.HandleStopAsync(Payload(), skipSessionFileLookup: false, cancellationToken);

        // assert
        Assert.True(outcome.Block);
        outcome.BlockReason!.Replace(actor, "<actor>").Replace(message.Id, "<message-id>").MatchInlineSnapshot(
            """
            Unread nitro mail is waiting; handle it before ending this turn, or ignore this once if it is not actionable right now.
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
    public async Task HandleStopAsync_Should_ReturnNeutral_When_StopHookActiveIsTrue()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);
        await SendMailAsync("bob", actor, cancellationToken);

        // act
        var outcome = await _venti.HandleStopAsync(
            Payload() with { StopHookActive = true }, skipSessionFileLookup: false, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
    }

    [Fact]
    public async Task HandleStopAsync_Should_StopBlocking_When_ThePerTurnBudgetIsExhausted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartAndGetActorAsync(cancellationToken);

        for (var i = 0; i < ClaudeHookHandler.MaxBlocksPerTurn; i++)
        {
            await SendMailAsync($"bob-{i}", actor, cancellationToken);
            await _venti.HandleStopAsync(Payload(), skipSessionFileLookup: false, cancellationToken);
        }

        await SendMailAsync("carol", actor, cancellationToken);

        // act
        var outcome = await _venti.HandleStopAsync(Payload(), skipSessionFileLookup: false, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
    }

    [Theory]
    [InlineData("permission_prompt")]
    [InlineData("agent_completed")]
    [InlineData("agent_needs_input")]
    public async Task HandleNotificationAsync_Should_LeaveTheStoreUntouched_When_TheTypeIsNotIdlePrompt(
        string notificationType)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var outcome = await _venti.HandleNotificationAsync(
            Payload() with { NotificationType = notificationType }, skipSessionFileLookup: false, cancellationToken);

        // assert
        Assert.Equal(ClaudeHookOutcome.Neutral, outcome);
        Assert.Null(await _agentStore.FindBySessionAsync(AgentSessionHarness.Venti, SessionId, cancellationToken));
    }

    [Fact]
    public async Task HandleNotificationAsync_Should_MintAndAnnounceTheActor_When_TheTypeIsIdlePrompt()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var outcome = await _venti.HandleNotificationAsync(
            Payload() with { NotificationType = "idle_prompt" }, skipSessionFileLookup: false, cancellationToken);

        // assert
        var row = await _agentStore.FindBySessionAsync(AgentSessionHarness.Venti, SessionId, cancellationToken);
        outcome.AdditionalContext!.Replace(row!.Name, "<actor>").MatchInlineSnapshot(
            """
            Your Nitro actor name is "<actor>". Pass this name to the `--actor` option to act under this actor explicitly.
            """);
    }

    [Theory]
    [InlineData("headless", 1)]
    [InlineData("interactive", 2)]
    public async Task Find_Should_ReturnNull_When_TheRegistryDoesNotAdvertiseAProtocol1InteractiveEndpoint(
        string kind,
        int protocol)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await WriteRegistryAsync(kind, protocol, cancellationToken);
        var reader = new VentiPeerRegistryReader(_peerDirectory);

        // act
        var session = reader.Find(SessionId);

        // assert
        Assert.Null(session);
    }

    private ClaudeHookHandler CreateHandler(string harness, IClaudeSessionFileReader sessionFileReader) => new(
        _fileSystem,
        _timeProvider,
        _agentStore,
        _ledger,
        _mail,
        harness,
        sessionFileReader);

    private ClaudeHookPayload Payload() => new() { SessionId = SessionId, Cwd = _workspaceRoot };

    private Task WriteRegistryAsync(string kind, int protocol, CancellationToken cancellationToken)
        => File.WriteAllTextAsync(
            Path.Combine(_peerDirectory, SessionId + ".json"),
            JsonSerializer.Serialize(
                new
                {
                    sessionId = SessionId,
                    pid = 4242,
                    kind,
                    messagingSocketPath = Path.Combine(_peerDirectory, "0011223344556677.sock"),
                    procStart = 1767960000000,
                    peerProtocol = protocol
                }),
            cancellationToken);

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }

    private async Task<string> StartAndGetActorAsync(CancellationToken cancellationToken)
    {
        await _venti.HandleSessionStartAsync(Payload(), skipSessionFileLookup: false, cancellationToken);

        return (await _agentStore.FindBySessionAsync(AgentSessionHarness.Venti, SessionId, cancellationToken))!.Name;
    }

    private async Task<MailMessage> SendMailAsync(string sender, string recipient, CancellationToken cancellationToken)
    {
        await using (var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken))
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO agents (name, registered_at, started_at, last_seen_at)
                VALUES (@name, @now, @now, @now)
                ON CONFLICT (name) DO NOTHING;
                """;
            command.Parameters.AddWithValue("@name", sender);
            command.Parameters.AddWithValue("@now", _timeProvider.GetUtcNow());
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = sender, Subject = "status", Body = "please check", To = [recipient] },
            cancellationToken);
    }
}
