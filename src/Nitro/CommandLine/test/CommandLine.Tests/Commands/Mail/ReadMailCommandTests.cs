namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

public sealed class ReadMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "read", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Print a message and mark it read.

            Usage:
              nitro agent mail read <message-id> [options]

            Arguments:
              <message-id>  The message ID

            Options:
              --thread         Print every message of the thread, oldest first, and mark them all read
              --actor <actor>  The acting identity used on mail commands (defaults to NITRO_MAIL_ACTOR, NITRO_TASK_ACTOR, or the OS user name)
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent mail read "m-abc123"
              nitro agent mail read "m-abc123" --thread
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "mail", "read", "m-abc123");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    [Fact]
    public async Task UnknownMessage_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "read", "m-missing");

        // assert
        result.AssertError(
            """
            Message 'm-missing' does not exist.
            """);
    }

    [Fact]
    public async Task NotSenderOrRecipient_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedAgentAsync("carol");
        var message = await SeedMessageAsync("bob", "Status", ["carol"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "read", message.Id);

        // assert
        result.AssertError(
            $"""
            'test-agent' is not the sender or a recipient of '{message.Id}' and cannot read it.
            """);
    }

    [Fact]
    public async Task PrintsHeadersAndBody_AndMarksRead()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync(
            "bob", "Status", ["test-agent"], body: "All good.");

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "read", message.Id);

        // assert
        result.AssertSuccess(
            $"""
            From: bob
            To: test-agent
            Date: 2026-01-01 00:00
            Subject: Status
            Thread: {message.Id}

            All good.
            """);
        var readAt = await QueryScalarAsync(
            "SELECT read_at FROM message_recipients "
            + $"WHERE message_id = '{message.Id}' AND recipient = 'test-agent'");
        Assert.NotNull(readAt);
    }

    [Fact]
    public async Task PrintsCc_WhenPresent()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedAgentAsync("carol");
        var message = await SeedMessageAsync(
            "bob", "Status", ["test-agent"], ["carol"], "All good.");

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "read", message.Id);

        // assert
        result.AssertSuccess(
            $"""
            From: bob
            To: test-agent
            Cc: carol
            Date: 2026-01-01 00:00
            Subject: Status
            Thread: {message.Id}

            All good.
            """);
    }

    [Fact]
    public async Task SenderWhoIsNotARecipient_ReadsWithoutMarkingAnyCopy()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync("test-agent", "Status", ["bob"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "read", message.Id);

        // assert
        Assert.Equal(0, result.ExitCode);
        var readAt = await QueryScalarAsync(
            "SELECT read_at FROM message_recipients "
            + $"WHERE message_id = '{message.Id}' AND recipient = 'bob'");
        Assert.Null(readAt);
    }

    [Fact]
    public async Task ThreadOption_PrintsEveryMessageChronologically_AndMarksAllRead()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var original = await SeedMessageAsync(
            "bob", "Status", ["test-agent"], body: "Ping.");
        FakeTime.Advance(TimeSpan.FromMinutes(5));
        await CreateStore().ReplyMessageAsync(
            original.Id, "test-agent", "Pong.", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "read", original.Id, "--thread");

        // assert
        result.AssertSuccess(
            $"""
            From: bob
            To: test-agent
            Date: 2026-01-01 00:00
            Subject: Status
            Thread: {original.Id}

            Ping.

            ---

            From: test-agent
            To: bob
            Date: 2026-01-01 00:05
            Subject: Status
            Thread: {original.Id}

            Pong.
            """);
        var readAt = await QueryScalarAsync(
            "SELECT read_at FROM message_recipients "
            + $"WHERE message_id = '{original.Id}' AND recipient = 'test-agent'");
        Assert.NotNull(readAt);
    }

    [Fact]
    public async Task JsonOutput_ReturnsMessageDetail()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync(
            "bob", "Status", ["test-agent"], body: "All good.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "read", message.Id);

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(message.Id, root.GetProperty("id").GetString());
        Assert.Equal("All good.", root.GetProperty("body").GetString());
        Assert.Equal(["test-agent"], root.GetProperty("to").EnumerateArray().Select(e => e.GetString()!).ToArray());
        Assert.True(root.GetProperty("read").GetBoolean());
        Assert.False(root.GetProperty("archived").GetBoolean());
    }

    [Fact]
    public async Task JsonOutput_Thread_ReturnsMessageArray()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var original = await SeedMessageAsync("bob", "Status", ["test-agent"]);
        FakeTime.Advance(TimeSpan.FromMinutes(5));
        await CreateStore().ReplyMessageAsync(
            original.Id, "test-agent", "Pong.", TestContext.Current.CancellationToken);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "read", original.Id, "--thread");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal(original.Id, items[0].GetProperty("id").GetString());
        Assert.Equal("Pong.", items[1].GetProperty("body").GetString());
    }
}
