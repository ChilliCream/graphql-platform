namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

public sealed class ThreadsMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "threads", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List threads the acting agent participates in, last activity first. Threads with archived messages are included.

            Usage:
              nitro agent mail threads [options]

            Options:
              --limit <limit>  The maximum number of messages to show
              --actor <actor>  The acting identity used on mail commands (defaults to NITRO_MAIL_ACTOR, NITRO_TASK_ACTOR, or the OS user name)
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent mail threads
              nitro agent mail threads --limit 10
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "mail", "threads");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    [Fact]
    public async Task EmptyThreads_PrintsFriendlyLine()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "threads");

        // assert
        result.AssertSuccess(
            """
            No threads.
            """);
    }

    [Fact]
    public async Task ListsThreads_LastActivityFirst()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var older = await SeedMessageAsync("bob", "Older thread", ["test-agent"]);
        FakeTime.Advance(TimeSpan.FromMinutes(5));
        var newer = await SeedMessageAsync("bob", "Newer thread", ["test-agent"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "threads");

        // assert
        var lines = result.StdOut.TrimEnd('\n').Split('\n');
        Assert.Equal(4, lines.Length);
        Assert.StartsWith(newer.ThreadId, lines[0]);
        Assert.StartsWith(older.ThreadId, lines[1]);
        Assert.Equal("2 thread(s)", lines[3]);
    }

    [Fact]
    public async Task ParticipantsColumn_ListsSenderAndRecipientsAcrossTheThread()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedAgentAsync("carol");
        var root = await SeedMessageAsync("bob", "Kickoff", ["test-agent", "carol"]);
        await CreateStore().ReplyMessageAsync(
            root.Id, "test-agent", "reply body", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "threads");

        // assert
        var line = result.StdOut.TrimEnd('\n').Split('\n')[0];
        Assert.Contains("bob,carol,test-agent", line);
    }

    [Fact]
    public async Task MessageCountAndUnreadCount_ReflectThreadAggregation()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var root = await SeedMessageAsync("bob", "Kickoff", ["test-agent"]);
        await CreateStore().MarkReadAsync(
            [root.Id], "test-agent", TestContext.Current.CancellationToken);
        await CreateStore().ReplyMessageAsync(
            root.Id, "bob", "follow up", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "threads");

        // assert
        var line = result.StdOut.TrimEnd('\n').Split('\n')[0];
        Assert.StartsWith($"{root.ThreadId}  *  Kickoff", line);
        Assert.Contains("  2  1  now", line);
    }

    [Fact]
    public async Task ArchivedMessages_AreIncluded()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync("bob", "Archived thread", ["test-agent"]);
        await CreateStore().ArchiveAsync(
            [message.Id], "test-agent", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "threads");

        // assert
        Assert.Contains("Archived thread", result.StdOut);
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
        var second = await SeedMessageAsync("bob", "Two", ["test-agent"]);
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        var third = await SeedMessageAsync("bob", "Three", ["test-agent"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "threads", "--limit", "2");

        // assert
        var lines = result.StdOut.TrimEnd('\n').Split('\n');
        Assert.Equal(4, lines.Length);
        Assert.StartsWith(third.ThreadId, lines[0]);
        Assert.StartsWith(second.ThreadId, lines[1]);
        Assert.Equal("2 thread(s)", lines[3]);
    }

    [Fact]
    public async Task LimitOption_ZeroOrNegative_RejectedAtParseTime()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var zeroResult = await ExecuteCommandAsync("agent", "mail", "threads", "--limit", "0");
        var negativeResult = await ExecuteCommandAsync("agent", "mail", "threads", "--limit", "-1");

        // assert
        Assert.Equal(1, zeroResult.ExitCode);
        Assert.Contains("Option '--limit' must be a positive number.", zeroResult.StdErr);
        Assert.Equal(1, negativeResult.ExitCode);
        Assert.Contains("Option '--limit' must be a positive number.", negativeResult.StdErr);
    }

    [Fact]
    public async Task JsonOutput_ReturnsThreadRows()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync("bob", "Status", ["test-agent"]);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "threads");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var item = document.RootElement.GetProperty("items")[0];

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(message.ThreadId, item.GetProperty("threadId").GetString());
        Assert.Equal("Status", item.GetProperty("subject").GetString());
        Assert.Equal(1, item.GetProperty("messageCount").GetInt32());
        Assert.Equal(1, item.GetProperty("unreadCount").GetInt32());

        var participants = item.GetProperty("participants")
            .EnumerateArray()
            .Select(e => e.GetString()!)
            .ToArray();
        Assert.Equal(["bob", "test-agent"], participants);
    }

    [Fact]
    public async Task JsonOutput_EmptyThreads_ReturnsEmptyItems()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "threads");

        // assert
        result.AssertSuccess(
            """
            {
              "items": []
            }
            """);
    }
}
