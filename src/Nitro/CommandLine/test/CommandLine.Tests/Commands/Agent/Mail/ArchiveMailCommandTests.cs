namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Mail;

public sealed class ArchiveMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "archive", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Archive one or more messages for the acting agent.

            Usage:
              nitro agent mail archive <message-ids>... [options]

            Arguments:
              <message-ids>  One or more message IDs

            Options:
              --actor <actor>  The actor performing this command; inferred from the current session when omitted
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent mail archive "m-abc123"
              nitro agent mail archive "m-abc123" "m-def456"
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "mail", "archive", "m-abc123");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    [Fact]
    public async Task SingleId_Archives()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync("bob", "Status", ["test-agent"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "archive", message.Id);

        // assert
        result.AssertSuccess($"✓ Archived '{message.Id}'.");
        var archivedAt = await QueryScalarAsync(
            "SELECT archived_at FROM message_recipients "
            + $"WHERE message_id = '{message.Id}' AND recipient = 'test-agent'");
        Assert.NotNull(archivedAt);
    }

    [Fact]
    public async Task MultipleIds_ArchivesAll()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var first = await SeedMessageAsync("bob", "One", ["test-agent"]);
        var second = await SeedMessageAsync("bob", "Two", ["test-agent"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "archive", first.Id, second.Id);

        // assert
        result.AssertSuccess(
            $"""
            ✓ Archived '{first.Id}'.
            ✓ Archived '{second.Id}'.
            """);
    }

    [Fact]
    public async Task UnknownId_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "archive", "m-missing");

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
        var result = await ExecuteCommandAsync("agent", "mail", "archive", mine.Id, foreign.Id);

        // assert
        result.AssertError($"'test-agent' is not a recipient of: {foreign.Id}.");
        var archivedAt = await QueryScalarAsync(
            "SELECT archived_at FROM message_recipients "
            + $"WHERE message_id = '{mine.Id}' AND recipient = 'test-agent'");
        Assert.Null(archivedAt);
    }

    [Fact]
    public async Task ArchivedMessage_IsExcludedFromDefaultInbox()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync("bob", "Status", ["test-agent"]);
        await ExecuteCommandAsync("agent", "mail", "archive", message.Id);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "inbox");

        // assert
        result.AssertSuccess(
            """
            No messages.
            """);
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
        var result = await ExecuteCommandAsync("agent", "mail", "archive", message.Id);

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
