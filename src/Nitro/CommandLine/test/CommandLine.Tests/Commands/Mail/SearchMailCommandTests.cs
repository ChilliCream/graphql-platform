namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

public sealed class SearchMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "search", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Search the acting agent's sent and received messages by subject, body, and sender, case-insensitively. Archived messages are included.

            Usage:
              nitro agent mail search <text> [options]

            Arguments:
              <text>  The text to search for in the subject and body

            Options:
              --limit <limit>  The maximum number of messages to show
              --actor <actor>  The actor performing this command; inferred from the current session when omitted
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent mail search "deploy"
              nitro agent mail search "deploy" --limit 10
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "mail", "search", "deploy");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    [Fact]
    public async Task EmptyResults_PrintsFriendlyLine()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "search", "nothing-to-find");

        // assert
        result.AssertSuccess(
            """
            No messages.
            """);
    }

    [Fact]
    public async Task FindsBySubject_CaseInsensitive()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedMessageAsync("bob", "Deploy the release", ["test-agent"]);
        await SeedMessageAsync("bob", "Unrelated", ["test-agent"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "search", "DEPLOY");

        // assert
        var lines = result.StdOut.TrimEnd('\n').Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Contains("Deploy the release", lines[0]);
        Assert.Equal("1 message(s)", lines[2]);
    }

    [Fact]
    public async Task FindsByBody_CaseInsensitive()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedMessageAsync(
            "bob", "Status", ["test-agent"], body: "the PARSER is fixed now");
        await SeedMessageAsync("bob", "Other", ["test-agent"], body: "nothing relevant");

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "search", "parser");

        // assert
        var lines = result.StdOut.TrimEnd('\n').Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Contains("Status", lines[0]);
        Assert.Equal("1 message(s)", lines[2]);
    }

    [Fact]
    public async Task ScopedToActor_NoHitsOnOtherAgentsPrivateMessages()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedAgentAsync("carol");
        await SeedMessageAsync("bob", "Private matter between bob and carol", ["carol"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "search", "Private");

        // assert
        result.AssertSuccess(
            """
            No messages.
            """);
    }

    [Fact]
    public async Task ArchivedMessages_AreIncluded()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync("bob", "Archived match", ["test-agent"]);
        await CreateStore().ArchiveAsync(
            [message.Id], "test-agent", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "search", "Archived");

        // assert
        Assert.Contains("Archived match", result.StdOut);
    }

    [Fact]
    public async Task PercentWildcard_IsEscapedAndMatchedLiterally()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedMessageAsync("bob", "100% done", ["test-agent"]);
        await SeedMessageAsync("bob", "100 other days", ["test-agent"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "search", "100%");

        // assert
        var lines = result.StdOut.TrimEnd('\n').Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Contains("100% done", lines[0]);
        Assert.Equal("1 message(s)", lines[2]);
    }

    [Fact]
    public async Task UnderscoreWildcard_IsEscapedAndMatchedLiterally()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedMessageAsync("bob", "a_c legacy id", ["test-agent"]);
        await SeedMessageAsync("bob", "abc unrelated id", ["test-agent"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "search", "a_c");

        // assert
        var lines = result.StdOut.TrimEnd('\n').Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Contains("a_c legacy id", lines[0]);
        Assert.Equal("1 message(s)", lines[2]);
    }

    [Fact]
    public async Task LimitOption_CapsResultCount()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedMessageAsync("bob", "Match one", ["test-agent"]);
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMessageAsync("bob", "Match two", ["test-agent"]);
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        var third = await SeedMessageAsync("bob", "Match three", ["test-agent"]);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "search", "Match", "--limit", "1");

        // assert
        var lines = result.StdOut.TrimEnd('\n').Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.StartsWith(third.Id, lines[0]);
        Assert.Equal("1 message(s)", lines[2]);
    }

    [Fact]
    public async Task LimitOption_ZeroOrNegative_RejectedAtParseTime()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var zeroResult = await ExecuteCommandAsync("agent", "mail", "search", "x", "--limit", "0");
        var negativeResult = await ExecuteCommandAsync(
            "agent", "mail", "search", "x", "--limit", "-1");

        // assert
        Assert.Equal(1, zeroResult.ExitCode);
        Assert.Contains("Option '--limit' must be a positive number.", zeroResult.StdErr);
        Assert.Equal(1, negativeResult.ExitCode);
        Assert.Contains("Option '--limit' must be a positive number.", negativeResult.StdErr);
    }

    [Fact]
    public async Task JsonOutput_ReturnsSearchRows()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        var message = await SeedMessageAsync("bob", "Status", ["test-agent"]);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "search", "Status");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var item = document.RootElement.GetProperty("items")[0];

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(message.Id, item.GetProperty("id").GetString());
        Assert.Equal(message.ThreadId, item.GetProperty("threadId").GetString());
        Assert.Equal("bob", item.GetProperty("from").GetString());
        Assert.Equal("Status", item.GetProperty("subject").GetString());
        Assert.False(item.GetProperty("read").GetBoolean());
        Assert.False(item.GetProperty("archived").GetBoolean());
    }

    [Fact]
    public async Task JsonOutput_EmptyResults_ReturnsEmptyItems()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "search", "nothing");

        // assert
        result.AssertSuccess(
            """
            {
              "items": []
            }
            """);
    }
}
