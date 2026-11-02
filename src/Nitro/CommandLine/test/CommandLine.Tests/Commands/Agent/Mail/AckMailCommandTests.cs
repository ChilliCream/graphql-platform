namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Mail;

public sealed class AckMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "ack", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Mark one or more messages read without printing them.

            Usage:
              nitro agent mail ack [options]

            Options:
              --message <message> (REQUIRED)  A message ID; repeat for several messages
              --actor <actor> (REQUIRED)      The actor performing this command; allocate one with `nitro agent login`
              --output <json>                 The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                  Show help and usage information

            Example:
              nitro agent mail ack --message "m-abc123" --actor "maya"
              nitro agent mail ack --message "m-abc123" --message "m-def456" --actor "maya"
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "mail", "ack", "--message", "m-abc123");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    [Fact]
    public async Task SingleId_MarksRead()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync("bob", "Status", ["test-agent"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "ack", "--message", message.Id);

        // assert
        result.AssertSuccess($"✓ Marked '{message.Id}' read.");
        var readAt = await QueryScalarAsync(
            "SELECT read_at FROM message_recipients "
            + $"WHERE message_id = '{message.Id}' AND recipient = 'test-agent'");
        Assert.NotNull(readAt);
    }

    [Fact]
    public async Task MultipleIds_MarksAllRead()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var first = await SeedMessageAsync("bob", "One", ["test-agent"]);
        var second = await SeedMessageAsync("bob", "Two", ["test-agent"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "ack", "--message", first.Id, "--message", second.Id);

        // assert
        result.AssertSuccess(
            $"""
            ✓ Marked '{first.Id}' read.
            ✓ Marked '{second.Id}' read.
            """);
    }

    [Fact]
    public async Task UnknownId_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "ack", "--message", "m-missing");

        // assert
        result.AssertError(
            """
            'test-agent' is not a recipient of: m-missing.
            """);
    }

    [Fact]
    public async Task ValidIdPlusForeignId_RollsBackBoth()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedAgentAsync("carol");
        var mine = await SeedMessageAsync("bob", "Mine", ["test-agent"]);
        var foreign = await SeedMessageAsync("bob", "Foreign", ["carol"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "ack", "--message", mine.Id, "--message", foreign.Id);

        // assert
        result.AssertError($"'test-agent' is not a recipient of: {foreign.Id}.");
        var readAt = await QueryScalarAsync(
            "SELECT read_at FROM message_recipients "
            + $"WHERE message_id = '{mine.Id}' AND recipient = 'test-agent'");
        Assert.Null(readAt);
    }

    [Fact]
    public async Task JsonOutput_ReturnsIds()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync("bob", "Status", ["test-agent"]);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "ack", "--message", message.Id);

        // assert
        result.AssertSuccess(
            $$"""
            {
              "ids": [
                "{{message.Id}}"
              ]
            }
            """);
    }
}
