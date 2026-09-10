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
              nitro agent mail reply [options]

            Options:
              --message <message> (REQUIRED)  The message ID
              --body <body>                   The message body; use --body-file to read it from a file instead
              --body-file <body-file>         A file to read the message body from; use it instead of --body
              --actor <actor> (REQUIRED)      The actor performing this command; allocate one with `nitro agent login`
              --output <json>                 The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                  Show help and usage information

            Example:
              nitro agent mail reply --message "m-abc123" --body "On it." --actor "maya"
              nitro agent mail reply --body-file reply.txt --message "m-abc123" --actor "maya"
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
            "agent", "mail", "reply", "--message", originalId, "--body", "Thanks!", "--actor", "bob");

        // assert
        var replyId = await QueryScalarAsync(
            "SELECT id FROM messages WHERE in_reply_to = '" + originalId + "'");
        result.AssertSuccess(
            $"""
            ✓ Sent '{replyId}' to alice, carol.
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
            "agent", "mail", "reply", "--message", originalId, "--body", "x", "--actor", "alice");

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
            "agent", "mail", "reply", "--message", originalId, "--body", "x", "--actor", "carol");

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
            "agent", "mail", "reply", "--message", "m-does-not-exist", "--body", "x");

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
            "agent", "mail", "reply", "--message", originalId, "--body", "Thanks!", "--actor", "bob");

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
