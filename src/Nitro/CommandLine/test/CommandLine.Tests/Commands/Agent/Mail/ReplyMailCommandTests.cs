using ChilliCream.Nitro.CommandLine.Tests.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Mail;

public sealed class ReplyMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "reply", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Reply to a message.

            Usage:
              nitro agent mail reply <message-id> [options]

            Arguments:
              <message-id>  The message ID

            Options:
              --body <body>            The message body. Exactly one of --body or --body-file is required
              --body-file <body-file>  A file to read the message body from. Exactly one of --body or --body-file is required
              --actor <actor>          The actor performing this command; inferred from the current session when omitted
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro agent mail reply "m-abc123" --body "On it."
              nitro agent mail reply "m-abc123" --body-file reply.txt
            """);
    }

    private async Task<string> SendOriginalMessageAsync(string sender, string subject, params string[] to)
    {
        var message = await SeedMessageAsync(sender, subject, to, body: "Original body.");
        return message.Id;
    }

    [Fact]
    public async Task ReplyAll_ExcludesSelf_IncludesSenderAndOtherRecipients()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alice");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await ExecuteCommandAsync("agent", "register", "--actor", "carol");
        var originalId = await SendOriginalMessageAsync("alice", "Status", "bob", "carol");
        await SetupSuccessfulWakeAsync("host-reply-all-test", "alice", "carol");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "reply", originalId, "--body", "Thanks!", "--actor", "bob");

        // assert
        var replyId = await QueryScalarAsync(
            "SELECT id FROM messages WHERE in_reply_to = '" + originalId + "'");
        result.AssertSuccess(
            $"""
            ✓ Sent '{replyId}' to alice, carol.
            wake delivered.
            """);
    }

    [Fact]
    public async Task SelfOnlyReply_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alice");
        var originalId = await SendOriginalMessageAsync("alice", "Note to self", "alice");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "reply", originalId, "--body", "x", "--actor", "alice");

        // assert
        result.AssertError(
            $"Replying to '{originalId}' as 'alice' would leave no recipients.");
    }

    [Fact]
    public async Task NonParticipant_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alice");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await ExecuteCommandAsync("agent", "register", "--actor", "carol");
        var originalId = await SendOriginalMessageAsync("alice", "Status", "bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "reply", originalId, "--body", "x", "--actor", "carol");

        // assert
        result.AssertError(
            $"'carol' is not the sender or a recipient of '{originalId}' and cannot reply to it.");
    }

    [Fact]
    public async Task NonexistentMessage_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "reply", "m-does-not-exist", "--body", "x");

        // assert
        result.AssertError(
            """
            Message 'm-does-not-exist' does not exist.
            """);
    }

    [Fact]
    public async Task Reply_ThreadsUnderOriginalMessage_AndInheritsRootSubject()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alice");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        var originalId = await SendOriginalMessageAsync("alice", "Root subject", "bob");
        await SetupSuccessfulWakeAsync("host-reply-thread-test", "alice");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "reply", originalId, "--body", "Thanks!", "--actor", "bob");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(originalId, root.GetProperty("threadId").GetString());
        Assert.Equal(originalId, root.GetProperty("inReplyTo").GetString());
        Assert.Equal("Root subject", root.GetProperty("subject").GetString());
        Assert.Equal(["alice"], root.GetProperty("to").EnumerateArray().Select(e => e.GetString()!).ToArray());
        Assert.True(root.GetProperty("messageStored").GetBoolean());
        Assert.Equal("delivered", root.GetProperty("notification").GetProperty("status").GetString());
    }

    [Fact]
    public async Task JsonOutput_Should_ReturnCleanJson_And_ExitNonzero_When_TheRecipientHasNoLiveSession()
    {
        // arrange: alice has never claimed a live session, so the
        // direct-first dispatcher has nobody to address at all - the reply
        // is durably stored but the wake is a confirmed failure.
        await InitWorkspaceAsync();
        SetupInstanceId("host-reply-nolive-test");
        await ExecuteCommandAsync("agent", "register", "--actor", "alice");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        var originalId = await SendOriginalMessageAsync("alice", "Status", "bob");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "reply", originalId, "--body", "Thanks!", "--actor", "bob");

        // assert: the notifier never alters mail's own exit code or stdout -
        // a single clean JSON result, nothing else, even though it now
        // reports a nonzero wake outcome.
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        Assert.Equal(["alice"], root.GetProperty("to").EnumerateArray().Select(e => e.GetString()!).ToArray());
        Assert.True(root.GetProperty("messageStored").GetBoolean());
        var notification = root.GetProperty("notification");
        Assert.Equal("failed", notification.GetProperty("status").GetString());
        var recipient = Assert.Single(notification.GetProperty("recipients").EnumerateArray());
        Assert.Equal("alice", recipient.GetProperty("actor").GetString());
        Assert.Equal("no-live-session", recipient.GetProperty("lastAttempt").GetProperty("reason").GetString());

        var pingResult = await QueryScalarAsync(
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-1'");
        Assert.Null(pingResult);
    }

    [Fact]
    public async Task HumanOutput_Should_ReportFailed_When_TheRecipientHasNoLiveSession()
    {
        // arrange: alice has never claimed a live session, so the
        // direct-first dispatcher has nobody to address at all - the reply
        // is durably stored but the wake is a confirmed failure.
        await InitWorkspaceAsync();
        SetupInstanceId("host-reply-nolive-human-test");
        await ExecuteCommandAsync("agent", "register", "--actor", "alice");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        var originalId = await SendOriginalMessageAsync("alice", "Status", "bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "reply", originalId, "--body", "Thanks!", "--actor", "bob");

        // assert
        var replyId = await QueryScalarAsync(
            "SELECT id FROM messages WHERE in_reply_to = '" + originalId + "'");
        result.AssertError(
            $"""
            Stored '{replyId}' to alice.
            message stored, but wake failed: no-live-session.
              alice: failed (no-live-session)
            """);
    }

    [Fact]
    public async Task HumanOutput_Should_ReportDelivered_When_TheWakeReachesALiveSession()
    {
        // arrange: alice has a live claimed codex-thread session and the
        // fake codex queue client reports success, so the reply's wake
        // delivers in the foreground.
        await InitWorkspaceAsync();
        const string host = "host-reply-delivered-human-test";
        SetupInstanceId(host);
        await ExecuteCommandAsync("agent", "register", "--actor", "alice");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SeedAliveCodexThreadSessionAsync("alice", "thread-alice", host);
        var originalId = await SendOriginalMessageAsync("alice", "Status", "bob");
        SetupCodexQueueClient(new FakeCodexQueueClient());

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "reply", originalId, "--body", "Thanks!", "--actor", "bob");

        // assert
        var replyId = await QueryScalarAsync(
            "SELECT id FROM messages WHERE in_reply_to = '" + originalId + "'");
        result.AssertSuccess(
            $"""
            ✓ Sent '{replyId}' to alice.
            wake delivered.
            """);
    }

    [Fact]
    public async Task JsonOutput_Should_ReportDelivered_When_TheWakeReachesALiveSession()
    {
        // arrange: alice has a live claimed codex-thread session and the
        // fake codex queue client reports success, so the reply's wake
        // delivers in the foreground.
        await InitWorkspaceAsync();
        const string host = "host-reply-delivered-test";
        SetupInstanceId(host);
        await ExecuteCommandAsync("agent", "register", "--actor", "alice");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SeedAliveCodexThreadSessionAsync("alice", "thread-alice", host);
        var originalId = await SendOriginalMessageAsync("alice", "Status", "bob");
        SetupCodexQueueClient(new FakeCodexQueueClient());
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "reply", originalId, "--body", "Thanks!", "--actor", "bob");

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        var notification = root.GetProperty("notification");
        Assert.Equal("delivered", notification.GetProperty("status").GetString());
        var recipient = Assert.Single(notification.GetProperty("recipients").EnumerateArray());
        Assert.Equal("alice", recipient.GetProperty("actor").GetString());
        Assert.Equal("delivered", recipient.GetProperty("status").GetString());

        var targetStatus = await QueryScalarAsync(
            "SELECT status FROM mail_wake_targets WHERE batch_id = "
            + $"(SELECT batch_id FROM mail_wake_batches WHERE actor = 'alice' AND nitro_instance_id = '{host}')");
        Assert.Equal("delivered", targetStatus);
    }

    [Fact]
    public async Task BodyAndBodyFileBothMissing_ReturnsParseError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alice");
        var originalId = await SendOriginalMessageAsync("alice", "Status", "alice");

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "reply", originalId);

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Exactly one of '--body' or '--body-file' is required.", result.StdErr);
    }
}
