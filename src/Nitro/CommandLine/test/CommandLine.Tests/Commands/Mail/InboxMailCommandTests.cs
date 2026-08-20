namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

public sealed class InboxMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "inbox", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List messages addressed to the acting agent, newest first.

            Usage:
              nitro agent mail inbox [options]

            Options:
              --unread         Only show messages that have not been read
              --from <from>    Only show messages sent by this agent
              --since <since>  Only show messages created at or after this RFC 3339 timestamp
              --all            Include archived messages
              --limit <limit>  The maximum number of messages to show
              --actor <actor>  The acting identity used on mail commands (defaults to NITRO_MAIL_ACTOR, NITRO_TASK_ACTOR, or the OS user name)
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent mail inbox
              nitro agent mail inbox --unread
              nitro agent mail inbox --from "agent-a" --since "2026-01-01T00:00:00Z"
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "mail", "inbox");

        // assert
        result.AssertError(
            """
            No mail workspace found. Run `nitro agent mail init` first.
            """);
    }

    [Fact]
    public async Task EmptyInbox_PrintsFriendlyLine()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "inbox");

        // assert
        result.AssertSuccess(
            """
            No messages.
            """);
    }

    [Fact]
    public async Task ListsMessages_NewestFirst()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedMessageAsync("bob", "First", ["test-agent"]);
        FakeTime.Advance(TimeSpan.FromMinutes(5));
        await SeedMessageAsync("bob", "Second", ["test-agent"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "inbox");

        // assert
        var lines = result.StdOut.TrimEnd('\n').Split('\n');
        Assert.Equal(4, lines.Length);
        Assert.Contains("Second", lines[0]);
        Assert.Contains("First", lines[1]);
        Assert.Equal("2 message(s)", lines[3]);
    }

    [Fact]
    public async Task ReadMessage_HasNoUnreadMarker()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync("bob", "Status", ["test-agent"]);
        await CreateStore().MarkReadAsync(
            [message.Id], "test-agent", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "inbox");

        // assert
        Assert.StartsWith($"{message.Id}     bob  Status  now", result.StdOut);
    }

    [Fact]
    public async Task UnreadMessage_HasUnreadMarker()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync("bob", "Status", ["test-agent"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "inbox");

        // assert
        Assert.StartsWith($"{message.Id}  *  bob  Status  now", result.StdOut);
    }

    [Fact]
    public async Task Default_ExcludesArchived_And_AllOption_IncludesThem()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync("bob", "Archived", ["test-agent"]);
        await CreateStore().ArchiveAsync(
            [message.Id], "test-agent", TestContext.Current.CancellationToken);

        // act
        var defaultResult = await ExecuteCommandAsync("agent", "mail", "inbox");
        var allResult = await ExecuteCommandAsync("agent", "mail", "inbox", "--all");

        // assert
        defaultResult.AssertSuccess(
            """
            No messages.
            """);
        Assert.Contains("Archived", allResult.StdOut);
    }

    [Fact]
    public async Task UnreadOption_FiltersToUnreadOnly()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var seen = await SeedMessageAsync("bob", "Seen", ["test-agent"]);
        await SeedMessageAsync("bob", "Fresh", ["test-agent"]);
        await CreateStore().MarkReadAsync(
            [seen.Id], "test-agent", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "inbox", "--unread");

        // assert
        var lines = result.StdOut.TrimEnd('\n').Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Contains("Fresh", lines[0]);
        Assert.Equal("1 message(s)", lines[2]);
    }

    [Fact]
    public async Task FromOption_FiltersBySender()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedAgentAsync("carol");
        await SeedMessageAsync("bob", "From bob", ["test-agent"]);
        await SeedMessageAsync("carol", "From carol", ["test-agent"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "inbox", "--from", "carol");

        // assert
        var lines = result.StdOut.TrimEnd('\n').Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Contains("From carol", lines[0]);
        Assert.Equal("1 message(s)", lines[2]);
    }

    [Fact]
    public async Task SinceOption_IsInclusiveOfTheBoundary()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedMessageAsync("bob", "Before", ["test-agent"]);
        FakeTime.Advance(TimeSpan.FromMinutes(5));
        var boundary = FakeTime.GetUtcNow();
        await SeedMessageAsync("bob", "AtBoundary", ["test-agent"]);
        FakeTime.Advance(TimeSpan.FromMinutes(5));
        await SeedMessageAsync("bob", "After", ["test-agent"]);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "inbox", "--since", boundary.ToString("O"));

        // assert
        var lines = result.StdOut.TrimEnd('\n').Split('\n');
        Assert.Equal(4, lines.Length);
        Assert.Contains("After", lines[0]);
        Assert.Contains("AtBoundary", lines[1]);
        Assert.Equal("2 message(s)", lines[3]);
    }

    [Fact]
    public async Task LimitOption_CapsResultCount()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedMessageAsync("bob", "One", ["test-agent"]);
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMessageAsync("bob", "Two", ["test-agent"]);
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMessageAsync("bob", "Three", ["test-agent"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "inbox", "--limit", "2");

        // assert
        var lines = result.StdOut.TrimEnd('\n').Split('\n');
        Assert.Equal(4, lines.Length);
        Assert.Contains("Three", lines[0]);
        Assert.Contains("Two", lines[1]);
        Assert.Equal("2 message(s)", lines[3]);
    }

    [Fact]
    public async Task LimitOption_ZeroOrNegative_RejectedAtParseTime()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var zeroResult = await ExecuteCommandAsync("agent", "mail", "inbox", "--limit", "0");
        var negativeResult = await ExecuteCommandAsync("agent", "mail", "inbox", "--limit", "-1");

        // assert
        Assert.Equal(1, zeroResult.ExitCode);
        Assert.Contains("Option '--limit' must be a positive number.", zeroResult.StdErr);
        Assert.Equal(1, negativeResult.ExitCode);
        Assert.Contains("Option '--limit' must be a positive number.", negativeResult.StdErr);
    }

    [Fact]
    public async Task AgeColumn_ReflectsElapsedTime()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync("bob", "Status", ["test-agent"]);
        FakeTime.Advance(TimeSpan.FromMinutes(5));

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "inbox");

        // assert
        Assert.StartsWith($"{message.Id}  *  bob  Status  5m", result.StdOut);
    }

    [Fact]
    public async Task JsonOutput_ReturnsInboxRows()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync("bob", "Status", ["test-agent"]);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "inbox");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var item = document.RootElement.GetProperty("items")[0];

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(message.Id, item.GetProperty("id").GetString());
        Assert.Equal(message.Id, item.GetProperty("threadId").GetString());
        Assert.Equal("bob", item.GetProperty("from").GetString());
        Assert.Equal("Status", item.GetProperty("subject").GetString());
        Assert.False(item.GetProperty("read").GetBoolean());
        Assert.False(item.GetProperty("archived").GetBoolean());
    }

    [Fact]
    public async Task JsonOutput_EmptyInbox_ReturnsEmptyItems()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "inbox");

        // assert
        result.AssertSuccess(
            """
            {
              "items": []
            }
            """);
    }
}
