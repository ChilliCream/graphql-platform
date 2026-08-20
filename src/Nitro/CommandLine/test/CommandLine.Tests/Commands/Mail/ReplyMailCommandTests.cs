namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

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
              --actor <actor>          The acting identity used on mail commands (defaults to NITRO_MAIL_ACTOR, NITRO_TASK_ACTOR, or the OS user name)
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro agent mail reply "m-abc123" --body "On it."
              nitro agent mail reply "m-abc123" --body-file reply.txt
            """);
    }

    private async Task<string> SendOriginalMessageAsync(string sender, string subject, params string[] to)
    {
        var args = new List<string>
        {
            "agent", "mail", "send"
        };
        args.AddRange(to);
        args.AddRange(["--subject", subject, "--body", "Original body.", "--actor", sender]);

        await ExecuteCommandAsync(args.ToArray());

        return (await QueryScalarAsync($"SELECT id FROM messages WHERE subject = '{subject}'"))!;
    }

    [Fact]
    public async Task ReplyAll_ExcludesSelf_IncludesSenderAndOtherRecipients()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "alice");
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "bob");
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "carol");
        var originalId = await SendOriginalMessageAsync("alice", "Status", "bob", "carol");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "reply", originalId, "--body", "Thanks!", "--actor", "bob");

        // assert
        var replyId = await QueryScalarAsync(
            "SELECT id FROM messages WHERE in_reply_to = '" + originalId + "'");
        result.AssertSuccess($"✓ Sent '{replyId}' to alice, carol.");
    }

    [Fact]
    public async Task SelfOnlyReply_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "alice");
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
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "alice");
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "bob");
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "carol");
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
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "alice");
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "bob");
        var originalId = await SendOriginalMessageAsync("alice", "Root subject", "bob");
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
    }

    [Fact]
    public async Task BodyAndBodyFileBothMissing_ReturnsParseError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "alice");
        var originalId = await SendOriginalMessageAsync("alice", "Status", "alice");

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "reply", originalId);

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Exactly one of '--body' or '--body-file' is required.", result.StdErr);
    }
}
