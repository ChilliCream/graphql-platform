namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

public sealed class BroadcastMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "broadcast", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Send a message to every registered agent except yourself.

            Usage:
              nitro agent mail broadcast [options]

            Options:
              --subject <subject> (REQUIRED)  The message subject
              --body <body>                   The message body. Exactly one of --body or --body-file is required
              --body-file <body-file>         A file to read the message body from. Exactly one of --body or --body-file is required
              --actor <actor>                 The acting identity used on mail commands (defaults to NITRO_MAIL_ACTOR, NITRO_TASK_ACTOR, or the OS user name)
              --output <json>                 The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                  Show help and usage information

            Example:
              nitro agent mail broadcast --subject "Heads up" --body "Deploying at 5pm."
            """);
    }

    [Fact]
    public async Task ExcludesSender_SendsToOthersOrderedByName()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "zeta");
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "alpha");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "Heads up", "--body", "Deploying.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Heads up'");
        result.AssertSuccess($"✓ Sent '{id}' to alpha, zeta.");
    }

    [Fact]
    public async Task NoOtherRegisteredAgent_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "solo");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "hi", "--body", "hello", "--actor", "solo");

        // assert
        result.AssertError(
            """
            No other registered agent to broadcast to.
            """);
    }

    [Fact]
    public async Task NoRegisteredAgentsAtAll_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "hi", "--body", "hello");

        // assert
        result.AssertError(
            """
            No other registered agent to broadcast to.
            """);
    }

    [Fact]
    public async Task JsonOutput_ReturnsMessageResult()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "bob");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "Heads up", "--body", "Deploying.");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("test-agent", root.GetProperty("from").GetString());
        Assert.Equal(["bob"], root.GetProperty("to").EnumerateArray().Select(e => e.GetString()!).ToArray());
    }

    [Fact]
    public async Task BodyAndBodyFileBothMissing_ReturnsParseError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "hi");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Exactly one of '--body' or '--body-file' is required.", result.StdErr);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "hi", "--body", "hello");

        // assert
        result.AssertError(
            """
            No mail workspace found. Run `nitro agent mail init` first.
            """);
    }
}
