using ChilliCream.Nitro.CommandLine.Services.Mail;

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
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.");

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
            "agent", "mail", "send", "bob", "carol", "--cc", "bob",
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
            "agent", "mail", "send", "dave", "eve", "--subject", "hi", "--body", "yo");

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
            "agent", "mail", "send", "bob", "dave", "--subject", "hi", "--body", "yo");

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
            "agent", "mail", "send", "dave", "--subject", "hi", "--body", "yo");

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
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.");

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
    }

    [Fact]
    public async Task JsonOutput_ReturnsUnregisteredRecipients()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "dave", "--subject", "Status", "--body", "All good.");

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
            "agent", "mail", "send", "bob", "--subject", "File body", "--body-file", "body.txt");

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
            "agent", "mail", "send", "test-agent", "--subject", "Note", "--body", "Remember this.");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            "test-agent",
            await QueryScalarAsync(
                "SELECT recipient FROM message_recipients WHERE recipient = 'test-agent'"));
    }

    [Fact]
    public async Task NoPing_Should_SkipTheNotifier_And_NeverInvokeTheLauncher()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInstanceId("host-send-noping-test");
        var launcher = new RecordingPingWorkerLauncher();
        SetupPingWorkerLauncher(launcher);
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SeedAliveCodexThreadSessionAsync("bob", "thread-bob", "host-send-noping-test");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--no-ping", "--subject", "Status", "--body", "All good.");

        // assert: --no-ping suppresses the notifier entirely, so the
        // launcher is never invoked and the session row is left untouched.
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(launcher.Calls);
        var pingResult = await QueryScalarAsync(
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-1'");
        Assert.Null(pingResult);
    }

    [Fact]
    public async Task JsonOutput_Should_ReturnCleanJson_When_TheRecipientHasNoOutstandingWakeGeneration()
    {
        // arrange: a recipient with a live claimed codex-thread session, but
        // send still defaults to MailWakePolicy.Skip, so the direct-first
        // dispatcher finds nothing outstanding and never touches the row.
        await InitWorkspaceAsync();
        SetupInstanceId("host-send-test");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SeedAliveCodexThreadSessionAsync("bob", "thread-bob", "host-send-test");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.");

        // assert: the notifier never alters mail's own exit code or stdout -
        // a single clean JSON result, nothing else.
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        Assert.Equal(["bob"], root.GetProperty("to").EnumerateArray().Select(e => e.GetString()!).ToArray());

        var pingResult = await QueryScalarAsync(
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-1'");
        Assert.Null(pingResult);
    }

    [Fact]
    public async Task WakeAttempt_Should_ReachACompletedBatchWithANonPendingTarget_When_ARecipientHasAnOutstandingGenerationAndALiveSession()
    {
        // arrange: an outstanding wake generation enqueued ahead of this
        // send (this send itself still defaults to MailWakePolicy.Skip)
        // plus a live claimed session proves Notifier -> the direct-first
        // dispatcher is wired end to end from a command.
        await InitWorkspaceAsync();
        const string host = "host-send-wake-attempt-test";
        SetupInstanceId(host);
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SeedAliveCodexThreadSessionAsync("bob", "thread-bob", host);
        await CreateWakeStore(host).SendMessageAsync(
            new MailMessageCreation
            {
                Sender = "test-agent",
                Subject = "earlier",
                Body = "unread",
                To = ["bob"],
                WakePolicy = MailWakePolicy.Enqueue
            },
            TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.");

        // assert: the dispatcher claimed and completed a batch for bob, and
        // its target row moved off pending, regardless of whether the
        // transport call itself actually succeeded.
        Assert.Equal(0, result.ExitCode);
        var batchStatus = await QueryScalarAsync(
            $"SELECT status FROM mail_wake_batches WHERE actor = 'bob' AND nitro_instance_id = '{host}'");
        Assert.Equal("completed", batchStatus);
        var targetStatus = await QueryScalarAsync(
            "SELECT status FROM mail_wake_targets WHERE batch_id = "
            + $"(SELECT batch_id FROM mail_wake_batches WHERE actor = 'bob' AND nitro_instance_id = '{host}')");
        Assert.NotEqual("pending", targetStatus);
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
