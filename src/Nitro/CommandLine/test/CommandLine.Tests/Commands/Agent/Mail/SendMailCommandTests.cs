using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Agents;
using ChilliCream.Nitro.CommandLine.Tests.Hook;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Mail;

public sealed class SendMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task NudgeAsync_Should_ReturnNormally_When_AgentLookupThrows()
    {
        // arrange
        var agentStore = new Mock<IAgentStore>();
        agentStore
            .Setup(store => store.FindAsync("bob", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("agent lookup failed"));
        var nudge = new MailNudge(
            agentStore.Object,
            Mock.Of<IMailStore>(),
            Mock.Of<IAgentDeliveryLedger>(),
            Mock.Of<IClaudePeerClient>(),
            Mock.Of<ICodexQueueClient>(),
            TimeProvider.System);

        // act
        await nudge.NudgeAsync(["bob"], TestContext.Current.CancellationToken);

        // assert
        agentStore.Verify(store => store.FindAsync("bob", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "send", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Send a message to one or more agents.

            Usage:
              nitro agent mail send [options]

            Options:
              --to <to> (REQUIRED)            A recipient agent name; repeat for several recipients
              --body <body>                   The message body; use --body-file to read it from a file instead
              --subject <subject> (REQUIRED)  The message subject
              --body-file <body-file>         A file to read the message body from; use it instead of --body
              --cc <cc>                       A recipient to carbon-copy; can be used multiple times
              --actor <actor> (REQUIRED)      The actor performing this command; allocate one with `nitro agent login`
              --output <json>                 The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                  Show help and usage information

            Example:
              nitro agent mail send --to "agent-a" --subject "Status" --body "All good." --actor "maya"
              nitro agent mail send --body-file notes.txt --to "agent-a" --to "agent-b" --cc "agent-c" --subject "Status" --actor "maya"
            """);
    }

    [Fact]
    public async Task Send_Should_DeliverMessage_When_ThereIsASingleRecipient()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        var queueClient = await SetupSuccessfulWakeAsync("bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--subject", "Status", "--body", "All good.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Status'");
        result.AssertSuccess(
            $"""
            ✓ Sent '{id}' to bob.
            """);
        Assert.Equal(
            ("thread-bob", id, "All good."),
            ReadDigestCall(Assert.Single(queueClient.Calls)));
    }

    [Fact]
    public async Task NudgeAsync_Should_SendPointer_When_TheSameMessageIsPushedTwiceToTheSameSession()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var queueClient = await SetupSuccessfulWakeAsync("bob");
        var message = await SeedMessageAsync(
            "test-agent", "Status", ["bob"], body: "All good.");
        var nudge = CreateMailNudge("host-send-repeat-test", queueClient);

        // act
        await nudge.NudgeAsync(["bob"], TestContext.Current.CancellationToken);
        await nudge.NudgeAsync(["bob"], TestContext.Current.CancellationToken);

        // assert
        Assert.Collection(
            queueClient.Calls,
            first => Assert.Equal(("thread-bob", message.Id, "All good."), ReadDigestCall(first)),
            second => Assert.Equal(
                ("thread-bob", "You have 1 unread nitro message. "
                    + "Run `nitro agent mail inbox --actor bob`."),
                (second.ThreadId, second.Message)));
    }

    [Fact]
    public async Task NudgeAsync_Should_LeaveTheMessageUnread_When_ItPushesTheBody()
    {
        // arrange
        // Pushing the body to the session never means it was read.
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var queueClient = await SetupSuccessfulWakeAsync("bob");
        var message = await SeedMessageAsync("test-agent", "Status", ["bob"], body: "All good.");
        var store = CreateStore();
        var cancellationToken = TestContext.Current.CancellationToken;
        var unreadBefore = await store.CountUnreadAsync("bob", cancellationToken);
        var nudge = CreateMailNudge("host-send-unread-test", queueClient);

        // act
        await nudge.NudgeAsync(["bob"], cancellationToken);

        // assert
        // The pushed payload says unread and the unread inbox and count are unchanged.
        var call = Assert.Single(queueClient.Calls);
        Assert.False(ReadDigestReadFlag(call));
        var unread = await store.QueryInboxAsync(
            new MailInboxFilter { Actor = "bob", UnreadOnly = true }, cancellationToken);
        Assert.Contains(unread, m => m.Id == message.Id);
        Assert.Equal(unreadBefore, await store.CountUnreadAsync("bob", cancellationToken));
    }

    [Fact]
    public async Task NudgeAsync_Should_ReserveNoDelivery_When_TheSessionIdIsMissingOnAClaudePeerEndpoint()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAliveSessionAsync(
            "session-alice", "alice", role: "",
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: "thread-alice");
        await SeedAliveSessionAsync(
            "session-bob", "bob", role: "",
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: "thread-bob");
        await ExecuteAsync(
            "UPDATE agents SET endpoint_kind = 'claude-peer', endpoint_addr = 'peer-addr', "
                + "session_id = NULL WHERE name = 'bob'");
        await SeedMessageAsync("alice", "Status", ["bob"], body: "All good.");
        var peerClient = new FakeClaudePeerClient();
        var nudge = new MailNudge(
            CreateAgentStore(),
            CreateStore(),
            new AgentDeliveryLedger(
                new ChilliCream.Nitro.CommandLine.Tests.Hook.TestFileSystem(WorkingDirectory), new AgentDatabase()),
            peerClient,
            new FakeCodexQueueClient(),
            FakeTime);

        // act
        await nudge.NudgeAsync(["bob"], TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(peerClient.Calls);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_deliveries WHERE agent = 'bob'"));
    }

    [Fact]
    public async Task NudgeAsync_Should_ReserveNoDelivery_When_TheEndpointKindIsOpencodeServerWithASession()
    {
        // arrange
        // SendAsync has no transport for opencode-server, even with a session.
        await InitWorkspaceAsync();
        await SeedAliveSessionAsync(
            "session-alice", "alice", role: "",
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: "thread-alice");
        await SeedAliveSessionAsync(
            "session-bob", "bob", role: "",
            endpointKind: AgentSessionEndpointKind.OpencodeServer, endpointAddr: "opencode-bob");
        await SeedMessageAsync("alice", "Status", ["bob"], body: "All good.");
        var codexQueueClient = new FakeCodexQueueClient();
        var nudge = new MailNudge(
            CreateAgentStore(),
            CreateStore(),
            new AgentDeliveryLedger(
                new ChilliCream.Nitro.CommandLine.Tests.Hook.TestFileSystem(WorkingDirectory), new AgentDatabase()),
            new FakeClaudePeerClient(),
            codexQueueClient,
            FakeTime);

        // act
        await nudge.NudgeAsync(["bob"], TestContext.Current.CancellationToken);

        // assert
        // No reservation was written, so a later nudge could still deliver it.
        Assert.Empty(codexQueueClient.Calls);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_deliveries WHERE agent = 'bob'"));
        var unread = await CreateStore().QueryInboxAsync(
            new MailInboxFilter { Actor = "bob", UnreadOnly = true }, TestContext.Current.CancellationToken);
        Assert.Single(unread);
    }

    [Fact]
    public async Task NudgeAsync_Should_ReserveADelivery_When_TheEndpointKindIsClaudePeerWithASession()
    {
        // arrange
        // A claude-peer endpoint with a session is a transport SendAsync can reach.
        await InitWorkspaceAsync();
        await SeedAliveSessionAsync(
            "session-alice", "alice", role: "",
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: "thread-alice");
        await SeedAliveSessionAsync(
            "session-bob", "bob", role: "",
            endpointKind: AgentSessionEndpointKind.ClaudePeer, endpointAddr: "peer-bob");
        var message = await SeedMessageAsync("alice", "Status", ["bob"], body: "All good.");
        var peerClient = new FakeClaudePeerClient();
        var nudge = new MailNudge(
            CreateAgentStore(),
            CreateStore(),
            new AgentDeliveryLedger(
                new ChilliCream.Nitro.CommandLine.Tests.Hook.TestFileSystem(WorkingDirectory), new AgentDatabase()),
            peerClient,
            new FakeCodexQueueClient(),
            FakeTime);

        // act
        await nudge.NudgeAsync(["bob"], TestContext.Current.CancellationToken);

        // assert
        var call = Assert.Single(peerClient.Calls);
        Assert.Equal(
            ("session-bob", message.Id, "All good."),
            ReadDigestCall((call.SessionId, call.Message)));
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM agent_deliveries WHERE agent = 'bob'"));
    }

    [Fact]
    public async Task SingleRecipient_Should_SendBodyToItsCurrentSession_When_AnEarlierSessionWasSuperseded()
    {
        // arrange
        // One agent row per actor, so a later session supersedes an earlier one.
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        var queueClient = new FakeCodexQueueClient();
        SetupCodexQueueClient(queueClient);
        await SeedAliveSessionAsync(
            "session-bob-1", "bob", role: "",
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: "thread-bob-1");
        await SeedAliveSessionAsync(
            "session-bob-2", "bob", role: "",
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: "thread-bob-2");

        // act
        await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--subject", "Status", "--body", "All good.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Status'");
        var call = Assert.Single(queueClient.Calls);
        Assert.Equal(("thread-bob-2", id!, "All good."), ReadDigestCall(call));
    }

    [Fact]
    public async Task Send_Should_CollapseNameInBothToAndCc_WithToWinning_When_RecipientsOverlap()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await ExecuteCommandAsync("agent", "register", "--actor", "carol");
        await SetupSuccessfulWakeAsync("bob", "carol");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--to", "carol", "--cc", "bob",
            "--subject", "Status", "--body", "All good.");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        var to = root.GetProperty("to").EnumerateArray().Select(e => e.GetString()!).ToArray();
        var cc = root.GetProperty("cc").EnumerateArray().Select(e => e.GetString()!).ToArray();
        Assert.Equal(["bob", "carol"], to);
        Assert.Empty(cc);
    }

    [Fact]
    public async Task Send_Should_ReturnUnknownAgentError_When_ToRecipientUnknown()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "dave", "--to", "eve", "--subject", "hi", "--body", "yo");

        // assert
        // The first offending name, in to-then-cc order, is reported.
        result.AssertError(
            """
            Unknown agent 'dave'. Look the name up with 'nitro agent list'.
            """);
        Assert.Null(await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'hi'"));
    }

    [Fact]
    public async Task Send_Should_ReturnUnknownAgentErrorAndWriteNothing_When_RecipientsMixKnownAndUnknown()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SetupSuccessfulWakeAsync("bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--to", "dave", "--subject", "hi", "--body", "yo");

        // assert
        result.AssertError(
            """
            Unknown agent 'dave'. Look the name up with 'nitro agent list'.
            """);
        Assert.Null(await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'hi'"));
    }

    [Fact]
    public async Task Send_Should_ReturnDeletedAgentError_When_ToRecipientDeleted()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("dave");
        await MarkAgentDeletedAsync("dave");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "dave", "--subject", "hi", "--body", "yo");

        // assert
        result.AssertError(
            """
            Agent 'dave' was deleted. Look the name up with 'nitro agent list'.
            """);
    }

    [Fact]
    public async Task InvalidRecipientName_StillHardFails()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "Dave!", "--subject", "hi", "--body", "yo");

        // assert
        result.AssertError(
            """
            Invalid agent name 'Dave!'. Agent names may only contain lowercase letters, digits, hyphens, and underscores.
            """);
    }

    [Fact]
    public async Task Send_Should_AllowRecipientToReadInbox_When_RecipientIsRegistered()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("dave");

        // act
        var sendResult = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "dave", "--subject", "hi", "--body", "yo");
        var inbox = await ExecuteCommandAsync("agent", "mail", "inbox", "--actor", "dave");

        // assert
        Assert.Equal(0, sendResult.ExitCode);
        Assert.Equal(0, inbox.ExitCode);
        Assert.Contains("hi", inbox.StdOut);
    }

    [Fact]
    public async Task Send_Should_ReturnSendResult_When_JsonOutputIsRequested()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SetupSuccessfulWakeAsync("bob");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--subject", "Status", "--body", "All good.");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.StartsWith("m-", root.GetProperty("id").GetString());
        Assert.Equal(root.GetProperty("id").GetString(), root.GetProperty("threadId").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("inReplyTo").ValueKind);
        Assert.Equal("test-agent", root.GetProperty("from").GetString());
        Assert.Equal(["bob"], root.GetProperty("to").EnumerateArray().Select(e => e.GetString()!).ToArray());
        Assert.Equal("Status", root.GetProperty("subject").GetString());
        Assert.True(root.TryGetProperty("createdAt", out _));
        Assert.True(root.GetProperty("messageStored").GetBoolean());
    }

    [Fact]
    public async Task BodyAndBodyFileBothMissing_ReturnsParseError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--subject", "hi");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Exactly one of '--body' or '--body-file' is required.", result.StdErr);
    }

    [Fact]
    public async Task BodyAndBodyFileBothGiven_ReturnsParseError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--subject", "hi",
            "--body", "x", "--body-file", "notes.txt");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Exactly one of '--body' or '--body-file' is required.", result.StdErr);
    }

    [Fact]
    public async Task EmptyBody_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--subject", "hi", "--body", "");

        // assert
        result.AssertError(
            """
            The '--body' option must not be empty.
            """);
    }

    [Fact]
    public async Task Send_Should_ReadBodyFileContentVerbatim_PreservingLineEndings_When_BodyFileIsGiven()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SetupSuccessfulWakeAsync("bob");
        var bodyFilePath = Path.Combine(WorkingDirectory, "body.txt");
        await File.WriteAllTextAsync(
            bodyFilePath, "Line one\r\nLine two\r\n", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--subject", "File body", "--body-file", "body.txt");

        // assert
        Assert.Equal(0, result.ExitCode);
        var body = await QueryScalarAsync("SELECT body FROM messages WHERE subject = 'File body'");
        Assert.Equal("Line one\r\nLine two\r\n", body);
    }

    [Fact]
    public async Task BodyFile_Empty_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        var bodyFilePath = Path.Combine(WorkingDirectory, "empty.txt");
        await File.WriteAllTextAsync(bodyFilePath, "", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--subject", "hi", "--body-file", "empty.txt");

        // assert
        result.AssertError(
            """
            The file 'empty.txt' is empty.
            """);
    }

    [Fact]
    public async Task BodyFile_DoesNotExist_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--subject", "hi", "--body-file", "missing.txt");

        // assert
        result.AssertError(
            """
            The file 'missing.txt' does not exist.
            """);
    }

    [Fact]
    public async Task Send_Should_BeAllowed_When_RecipientIsSelf()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register");
        await SetupSuccessfulWakeAsync("test-agent");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "test-agent", "--subject", "Note", "--body", "Remember this.");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            "test-agent",
            await QueryScalarAsync(
                "SELECT recipient FROM message_recipients WHERE recipient = 'test-agent'"));
    }

    [Fact]
    public async Task NoPing_Should_BeRejected_When_Provided()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--no-ping", "--subject", "Status", "--body", "All good.");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Invalid agent name '--no-ping'.",
            result.StdOut + result.StdErr,
            StringComparison.Ordinal);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM messages"));
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--subject", "hi", "--body", "yo");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }
}
