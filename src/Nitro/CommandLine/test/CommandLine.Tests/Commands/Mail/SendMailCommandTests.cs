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
    public async Task UnknownRecipients_ReturnsErrorInFirstOccurrenceOrder()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "dave", "eve", "--subject", "hi", "--body", "yo");

        // assert
        result.AssertError(
            """
            Unknown recipient(s): dave, eve.
            """);
    }

    [Fact]
    public async Task JsonOutput_ReturnsMessageResult()
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
