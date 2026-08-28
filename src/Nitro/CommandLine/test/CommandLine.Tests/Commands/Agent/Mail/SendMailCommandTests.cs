using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Agents;
using ChilliCream.Nitro.CommandLine.Tests.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Mail;

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
    public async Task SingleRecipient_SendsMessage()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SetupSuccessfulWakeAsync("host-send-single-test", "bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--subject", "Status", "--body", "All good.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Status'");
        result.AssertSuccess(
            $"""
            ✓ Sent '{id}' to bob.
            """);
    }

    [Fact]
    public async Task NameInBothToAndCc_CollapsesWithToWinning()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await ExecuteCommandAsync("agent", "register", "--actor", "carol");
        await SetupSuccessfulWakeAsync("host-send-dedupe-test", "bob", "carol");
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
    public async Task UnknownRecipients_SendsAndWarnsInFirstOccurrenceOrder()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "dave", "--to", "eve", "--subject", "hi", "--body", "yo");

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
        await SetupSuccessfulWakeAsync("host-send-known-unknown-test", "bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--to", "dave", "--subject", "hi", "--body", "yo");

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
            "agent", "mail", "send", "--to", "Dave!", "--subject", "hi", "--body", "yo");

        // assert
        result.AssertError(
            """
            Invalid agent name 'Dave!'. Agent names may only contain lowercase letters, digits, hyphens, and underscores.
            """);
    }

    [Fact]
    public async Task ImplicitRecipient_CanReadInbox_AndCanRegister()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "dave", "--subject", "hi", "--body", "yo");

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
    }

    [Fact]
    public async Task JsonOutput_ReturnsSendResult()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("bob");
        await SetupSuccessfulWakeAsync("host-send-json-test", "bob");
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
        Assert.Empty(root.GetProperty("unregistered").EnumerateArray());
        Assert.True(root.GetProperty("messageStored").GetBoolean());
    }

    [Fact]
    public async Task JsonOutput_ReturnsUnregisteredRecipients()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "dave", "--subject", "Status", "--body", "All good.");

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
    public async Task BodyFile_ReadsContentVerbatim_PreservingLineEndings()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SetupSuccessfulWakeAsync("host-send-body-file-test", "bob");
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
    public async Task SendingToSelf_IsAllowed()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register");
        await SetupSuccessfulWakeAsync("host-send-self-test", "test-agent");

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
