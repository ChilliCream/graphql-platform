namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Mail;

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
              nitro agent mail read [options]

            Options:
              --message <message> (REQUIRED)  The message ID
              --thread                        Print every message of the thread, oldest first, and mark them all read
              --actor <actor> (REQUIRED)      The actor performing this command; allocate one with `nitro agent login`
              --output <json>                 The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                  Show help and usage information

            Example:
              nitro agent mail read --message "m-abc123" --actor "maya"
              nitro agent mail read --message "m-abc123" --thread --actor "maya"
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "mail", "read", "--message", "m-abc123");

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
        var result = await ExecuteCommandAsync("agent", "mail", "read", "--message", "m-missing");

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
        var result = await ExecuteCommandAsync("agent", "mail", "read", "--message", message.Id);

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
        var result = await ExecuteCommandAsync("agent", "mail", "read", "--message", message.Id);

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
        var result = await ExecuteCommandAsync("agent", "mail", "read", "--message", message.Id);

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
        var result = await ExecuteCommandAsync("agent", "mail", "read", "--message", message.Id);

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
            "agent", "mail", "read", "--message", original.Id, "--thread");

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
        var result = await ExecuteCommandAsync("agent", "mail", "read", "--message", message.Id);

        // assert
        result.AssertSuccess(
            $$"""
            {
              "id": "{{message.Id}}",
              "threadId": "{{message.Id}}",
              "inReplyTo": null,
              "from": "bob",
              "to": [
                "test-agent"
              ],
              "cc": [],
              "subject": "Status",
              "body": "All good.",
              "createdAt": "2026-01-01T00:00:00+00:00",
              "read": true,
              "archived": false,
              "takeovers": []
            }
            """);
    }

    [Fact]
    public async Task Read_Should_PrintTwoTakeoverHopsNewestFirst()
    {
        // arrange
        var history = await SeedTakeoverHistoryAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "read", "--message", history.Message.Id, "--actor", "zoe");

        // assert
        result.AssertSuccess(
            $"""
            From: bob
            To: zoe
            Date: 2026-01-01 00:00
            Subject: Status
            Thread: {history.Message.Id}
            Takeover: nora -> zoe ({history.LatestId}, 2026-01-02)
            Takeover: maya -> nora ({history.EarliestId}, 2026-01-01)

            All good.
            """);
    }

    [Fact]
    public async Task Read_Should_ReturnTwoTakeoverHopsInJsonNewestFirst()
    {
        // arrange
        var history = await SeedTakeoverHistoryAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "read", "--message", history.Message.Id, "--actor", "zoe");

        // assert
        result.AssertSuccess(
            $$"""
            {
              "id": "{{history.Message.Id}}",
              "threadId": "{{history.Message.Id}}",
              "inReplyTo": null,
              "from": "bob",
              "to": [
                "zoe"
              ],
              "cc": [],
              "subject": "Status",
              "body": "All good.",
              "createdAt": "2026-01-01T00:00:00+00:00",
              "read": true,
              "archived": false,
              "takeovers": [
                {
                  "id": "{{history.LatestId}}",
                  "from": "nora",
                  "to": "zoe",
                  "createdAt": "2026-01-02T00:00:00+00:00"
                },
                {
                  "id": "{{history.EarliestId}}",
                  "from": "maya",
                  "to": "nora",
                  "createdAt": "2026-01-01T00:00:00+00:00"
                }
              ]
            }
            """);
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
            "agent", "mail", "read", "--message", original.Id, "--thread");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal(original.Id, items[0].GetProperty("id").GetString());
        Assert.Equal("Pong.", items[1].GetProperty("body").GetString());
    }

    private async Task<TakeoverHistory> SeedTakeoverHistoryAsync()
    {
        await InitWorkspaceAsync();
        await SeedAgentAsync("maya");
        await SeedAgentAsync("nora");
        await SeedAgentAsync("zoe");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync(
            "bob", "Status", ["maya"], body: "All good.");

        await ExecuteCommandAsync("agent", "takeover", "--from", "maya", "--actor", "nora");
        var earliestId = await QueryScalarAsync("SELECT id FROM agent_takeovers");

        FakeTime.Advance(TimeSpan.FromDays(1));
        await ExecuteCommandAsync("agent", "takeover", "--from", "nora", "--actor", "zoe");
        var latestId = await QueryScalarAsync(
            "SELECT id FROM agent_takeovers ORDER BY created_at DESC LIMIT 1");

        return new TakeoverHistory(message, earliestId!, latestId!);
    }

    private async Task<string?> QueryScalarAsync(string sql)
    {
        await using var connection =
            new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
        return result is null or DBNull ? null : result.ToString();
    }

    private sealed record TakeoverHistory(
        ChilliCream.Nitro.CommandLine.Services.Mail.MailMessage Message,
        string EarliestId,
        string LatestId);
}
