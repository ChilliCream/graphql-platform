using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Agents;
using ChilliCream.Nitro.CommandLine.Tests.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

public sealed class SendMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
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
              nitro agent mail send <recipients>... [options]

            Arguments:
              <recipients>  One or more recipient agent names

            Options:
              --subject <subject> (REQUIRED)  The message subject
              --body <body>                   The message body. Exactly one of --body or --body-file is required
              --body-file <body-file>         A file to read the message body from. Exactly one of --body or --body-file is required
              --cc <cc>                       A recipient to carbon-copy; can be used multiple times
              --actor <actor>                 The acting identity used on mail commands (defaults to NITRO_MAIL_ACTOR, NITRO_TASK_ACTOR, or the OS user name)
              --no-ping                       Skip the best-effort wake ping to recipients with a live claimed session
              --output <json>                 The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                  Show help and usage information

            Example:
              nitro agent mail send "agent-a" --subject "Status" --body "All good."
              nitro agent mail send "agent-a" "agent-b" --cc "agent-c" --subject "Status" --body-file notes.txt
            """);
    }

    [Fact]
    public async Task SingleRecipient_SendsMessage()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--no-ping", "--subject", "Status", "--body", "All good.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Status'");
        result.AssertSuccess($"✓ Sent '{id}' to bob.");
    }

    [Fact]
    public async Task NameInBothToAndCc_CollapsesWithToWinning()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await ExecuteCommandAsync("agent", "register", "--actor", "carol");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "carol", "--cc", "bob", "--no-ping",
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
    public async Task UnknownRecipients_SendsAndWarnsInFirstOccurrenceOrder()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "dave", "eve", "--no-ping", "--subject", "hi", "--body", "yo");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'hi'");
        result.AssertSuccess(
            $"""
            ✓ Sent '{id}' to dave, eve.
            note: 'dave' has never registered.
            note: 'eve' has never registered.
            """);
        Assert.Equal(
            "1",
            await QueryScalarAsync("SELECT implicit FROM agents WHERE name = 'dave'"));
        Assert.Equal(
            "1",
            await QueryScalarAsync("SELECT implicit FROM agents WHERE name = 'eve'"));
    }

    [Fact]
    public async Task MixOfKnownAndUnknownRecipients_WarnsOnlyOnUnknown()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "dave", "--no-ping", "--subject", "hi", "--body", "yo");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'hi'");
        result.AssertSuccess(
            $"""
            ✓ Sent '{id}' to bob, dave.
            note: 'dave' has never registered.
            """);
    }

    [Fact]
    public async Task InvalidRecipientName_StillHardFails()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "Dave!", "--subject", "hi", "--body", "yo");

        // assert
        result.AssertError(
            """
            Invalid agent name 'Dave!'. Agent names may only contain lowercase letters, digits, hyphens, and underscores.
            """);
    }

    [Fact]
    public async Task ImplicitRecipient_CanReadInboxBeforeAndAfterRegistering()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync(
            "agent", "mail", "send", "dave", "--no-ping", "--subject", "hi", "--body", "yo");

        // act
        var beforeRegister = await ExecuteCommandAsync("agent", "mail", "inbox", "--actor", "dave");

        // assert
        Assert.Equal(0, beforeRegister.ExitCode);
        Assert.Contains("hi", beforeRegister.StdOut);

        // act
        var registerResult = await ExecuteCommandAsync("agent", "register", "--actor", "dave");
        var afterRegister = await ExecuteCommandAsync("agent", "mail", "inbox", "--actor", "dave");

        // assert
        Assert.Equal(0, registerResult.ExitCode);
        Assert.Equal(0, afterRegister.ExitCode);
        Assert.Contains("hi", afterRegister.StdOut);
        Assert.Equal(
            "0",
            await QueryScalarAsync("SELECT implicit FROM agents WHERE name = 'dave'"));
    }

    [Fact]
    public async Task JsonOutput_ReturnsSendResult()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--no-ping", "--subject", "Status", "--body", "All good.");

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
        Assert.Empty(root.GetProperty("unregistered").EnumerateArray());
        Assert.True(root.GetProperty("messageStored").GetBoolean());
        var notification = root.GetProperty("notification");
        Assert.Equal("skipped", notification.GetProperty("status").GetString());
        Assert.False(notification.GetProperty("deliveryPending").GetBoolean());
        var recipient = Assert.Single(notification.GetProperty("recipients").EnumerateArray());
        Assert.Equal("bob", recipient.GetProperty("actor").GetString());
        Assert.Equal("skipped", recipient.GetProperty("status").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, recipient.GetProperty("wakeGeneration").ValueKind);
    }

    [Fact]
    public async Task JsonOutput_ReturnsUnregisteredRecipients()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "dave", "--no-ping", "--subject", "Status", "--body", "All good.");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            ["dave"], root.GetProperty("unregistered").EnumerateArray().Select(e => e.GetString()!).ToArray());
    }

    [Fact]
    public async Task BodyAndBodyFileBothMissing_ReturnsParseError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "hi");

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
            "agent", "mail", "send", "bob", "--subject", "hi",
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
            "agent", "mail", "send", "bob", "--subject", "hi", "--body", "");

        // assert
        result.AssertError(
            """
            The '--body' option must not be empty.
            """);
    }

    [Fact]
    public async Task BodyFile_ReadsContentVerbatim_PreservingLineEndings()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        var bodyFilePath = Path.Combine(WorkingDirectory, "body.txt");
        await File.WriteAllTextAsync(
            bodyFilePath, "Line one\r\nLine two\r\n", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--no-ping", "--subject", "File body", "--body-file", "body.txt");

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
            "agent", "mail", "send", "bob", "--subject", "hi", "--body-file", "empty.txt");

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
            "agent", "mail", "send", "bob", "--subject", "hi", "--body-file", "missing.txt");

        // assert
        result.AssertError(
            """
            The file 'missing.txt' does not exist.
            """);
    }

    [Fact]
    public async Task SendingToSelf_IsAllowed()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "test-agent", "--no-ping", "--subject", "Note", "--body", "Remember this.");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            "test-agent",
            await QueryScalarAsync(
                "SELECT recipient FROM message_recipients WHERE recipient = 'test-agent'"));
    }

    [Fact]
    public async Task NoPing_Should_SkipWakeEntirely_And_NeverCreateAWakeGeneration()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInstanceId("host-send-noping-test");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SeedAliveCodexThreadSessionAsync("bob", "thread-bob", "host-send-noping-test");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--no-ping", "--subject", "Status", "--body", "All good.");

        // assert: --no-ping stores with MailWakePolicy.Skip, so no wake
        // generation is ever created for bob and the live session is left
        // untouched.
        Assert.Equal(0, result.ExitCode);
        var outboxRows = await QueryScalarAsync(
            "SELECT COUNT(*) FROM mail_wake_outbox WHERE actor = 'bob' AND nitro_instance_id = 'host-send-noping-test'");
        Assert.Equal("0", outboxRows);
        var pingResult = await QueryScalarAsync(
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-1'");
        Assert.Null(pingResult);
    }

    [Fact]
    public async Task JsonOutput_Should_ReturnCleanJson_And_ExitNonzero_When_TheRecipientHasNoLiveSession()
    {
        // arrange: bob is registered but has never claimed a live session,
        // so the direct-first dispatcher has nobody to address at all - the
        // message is durably stored but the wake is a confirmed failure, not
        // silent success.
        await InitWorkspaceAsync();
        SetupInstanceId("host-send-nolive-test");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.");

        // assert: exactly one clean JSON object on stdout, nothing on
        // stderr, even though the command exits nonzero.
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        Assert.StartsWith("m-", root.GetProperty("id").GetString());
        Assert.True(root.GetProperty("messageStored").GetBoolean());
        var notification = root.GetProperty("notification");
        Assert.Equal("failed", notification.GetProperty("status").GetString());
        Assert.False(notification.GetProperty("deliveryPending").GetBoolean());
        var recipient = Assert.Single(notification.GetProperty("recipients").EnumerateArray());
        Assert.Equal("bob", recipient.GetProperty("actor").GetString());
        Assert.Equal("failed", recipient.GetProperty("status").GetString());
        Assert.Equal("no-live-session", recipient.GetProperty("lastAttempt").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task HumanOutput_Should_ExitNonzero_And_WriteToStderrOnly_When_TheRecipientHasNoLiveSession()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInstanceId("host-send-nolive-human-test");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.");

        // assert: the message is stored but the wake failed - human text
        // says so on stderr and stdout stays empty, matching how every
        // other nonzero mail outcome reports.
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Status'");
        result.AssertError(
            $"""
            Stored '{id}' to bob.
            message stored, but wake failed: no-live-session.
              bob: failed (no-live-session)
            """);
    }

    [Fact]
    public async Task JsonOutput_Should_ReportDelivered_When_TheWakeReachesALiveSession()
    {
        // arrange: bob has a live claimed codex-thread session and the fake
        // codex queue client reports success, so the direct-first dispatcher
        // delivers the wake in the foreground.
        await InitWorkspaceAsync();
        const string host = "host-send-delivered-test";
        SetupInstanceId(host);
        SetupCodexQueueClient(new FakeCodexQueueClient());
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SeedAliveCodexThreadSessionAsync("bob", "thread-bob", host);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.");

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        Assert.True(root.GetProperty("messageStored").GetBoolean());
        var notification = root.GetProperty("notification");
        Assert.Equal("delivered", notification.GetProperty("status").GetString());
        Assert.False(notification.GetProperty("deliveryPending").GetBoolean());
        var recipient = Assert.Single(notification.GetProperty("recipients").EnumerateArray());
        Assert.Equal("bob", recipient.GetProperty("actor").GetString());
        Assert.Equal("delivered", recipient.GetProperty("status").GetString());
        Assert.True(recipient.GetProperty("wakeGeneration").GetInt64() > 0);

        // the wake-driven ping never touches last_ping_result - that column
        // stays owned by `nitro agent ping`.
        var pingResult = await QueryScalarAsync(
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-1'");
        Assert.Null(pingResult);

        var targetStatus = await QueryScalarAsync(
            "SELECT status FROM mail_wake_targets WHERE batch_id = "
            + $"(SELECT batch_id FROM mail_wake_batches WHERE actor = 'bob' AND nitro_instance_id = '{host}')");
        Assert.Equal("delivered", targetStatus);
    }

    [Fact]
    public async Task HumanOutput_Should_ReportDelivered_When_TheWakeReachesALiveSession()
    {
        // arrange: bob has a live claimed codex-thread session and the fake
        // codex queue client reports success, so the direct-first dispatcher
        // delivers the wake in the foreground.
        await InitWorkspaceAsync();
        const string host = "host-send-delivered-human-test";
        SetupInstanceId(host);
        SetupCodexQueueClient(new FakeCodexQueueClient());
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SeedAliveCodexThreadSessionAsync("bob", "thread-bob", host);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Status'");
        result.AssertSuccess(
            $"""
            ✓ Sent '{id}' to bob.
            wake delivered.
            """);
    }

    [Fact]
    public async Task JsonOutput_Should_ReportPartial_When_ARecipientHasOneDeliveredAndOneFailingSession()
    {
        // arrange: alpha has a single live codex-thread session that
        // delivers. bob has two live sessions - one codex-thread session
        // that also delivers and one with no endpoint at all - so bob's own
        // recipient status aggregates to "partial". Sent in alpha-then-bob
        // order, this proves the command's own exit is controlled by bob's
        // partial status even though alpha, listed first, delivered cleanly,
        // and that the targets array is returned in deterministic
        // (harness, sessionId) order.
        await InitWorkspaceAsync();
        const string host = "host-send-partial-recipient-test";
        SetupInstanceId(host);
        SetupCodexQueueClient(new FakeCodexQueueClient());
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SeedAliveSessionAsync(
            "session-alpha", "alpha", role: "", host,
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: "thread-alpha");
        await SeedAliveSessionAsync(
            "session-bob-1", "bob", role: "", host,
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: "thread-bob");
        await SeedAliveSessionAsync(
            "session-bob-2", "bob", role: "", host,
            endpointKind: AgentSessionEndpointKind.None);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "alpha", "bob", "--subject", "Status", "--body", "All good.");

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        var notification = root.GetProperty("notification");
        Assert.Equal("partial", notification.GetProperty("status").GetString());
        var recipients = notification.GetProperty("recipients").EnumerateArray().ToArray();
        Assert.Equal(2, recipients.Length);
        Assert.Equal("alpha", recipients[0].GetProperty("actor").GetString());
        Assert.Equal("delivered", recipients[0].GetProperty("status").GetString());
        Assert.Equal("bob", recipients[1].GetProperty("actor").GetString());
        Assert.Equal("partial", recipients[1].GetProperty("status").GetString());

        var targets = recipients[1].GetProperty("targets").EnumerateArray().ToArray();
        Assert.Equal(2, targets.Length);
        Assert.Equal("session-bob-1", targets[0].GetProperty("sessionId").GetString());
        Assert.Equal("delivered", targets[0].GetProperty("status").GetString());
        Assert.Equal("session-bob-2", targets[1].GetProperty("sessionId").GetString());
        Assert.Equal("failed", targets[1].GetProperty("status").GetString());
        Assert.Equal(
            "no-endpoint", targets[1].GetProperty("lastAttempt").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task JsonOutput_Should_ReportPending_When_ClaudeAccessIsDeniedWithoutAcknowledgement()
    {
        // arrange: bob has a live claimed Claude peer session, but the peer
        // socket connect itself is denied. With no dashboard leader running
        // to accept responsibility (out of this ticket's scope), the offer
        // stays unacknowledged - durably pending, not silently OK.
        await InitWorkspaceAsync();
        const string host = "host-send-access-denied-test";
        SetupInstanceId(host);
        SetupClaudePeerClient(new FakeClaudePeerClient { NextOutcome = ClaudePeerSendOutcome.AccessDenied });
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SeedAliveSessionAsync(
            "session-1", "bob", role: "", host,
            endpointKind: AgentSessionEndpointKind.ClaudePeer, endpointAddr: "peer-a");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.");

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        Assert.True(root.GetProperty("messageStored").GetBoolean());
        var notification = root.GetProperty("notification");
        Assert.Equal("pending", notification.GetProperty("status").GetString());
        Assert.True(notification.GetProperty("deliveryPending").GetBoolean());
        var recipient = Assert.Single(notification.GetProperty("recipients").EnumerateArray());
        Assert.Equal("pending", recipient.GetProperty("status").GetString());
        Assert.Equal("access-denied", recipient.GetProperty("lastAttempt").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task HumanOutput_Should_ReportPending_When_ClaudeAccessIsDeniedWithoutAcknowledgement()
    {
        // arrange: bob has a live claimed Claude peer session, but the peer
        // socket connect itself is denied. With no dashboard leader running
        // to accept responsibility (out of this ticket's scope), the offer
        // stays unacknowledged - durably pending, not silently OK.
        await InitWorkspaceAsync();
        const string host = "host-send-access-denied-human-test";
        SetupInstanceId(host);
        SetupClaudePeerClient(new FakeClaudePeerClient { NextOutcome = ClaudePeerSendOutcome.AccessDenied });
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SeedAliveSessionAsync(
            "session-1", "bob", role: "", host,
            endpointKind: AgentSessionEndpointKind.ClaudePeer, endpointAddr: "peer-a");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Status'");
        result.AssertError(
            $"""
            Stored '{id}' to bob.
            message stored but wake remains unconfirmed.
              bob: pending (access-denied)
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "hi", "--body", "yo");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }
}
