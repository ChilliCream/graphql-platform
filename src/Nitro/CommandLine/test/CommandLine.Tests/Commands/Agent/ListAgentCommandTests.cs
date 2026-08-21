namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class ListAgentCommandTests(NitroCommandFixture fixture)
    : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "list", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List registered agents.

            Usage:
              nitro agent list [options]

            Options:
              --role <role>    The agent's role, free text, normalized lowercase (defaults to empty)
              --stale          Only show agents not seen in the last 30 days
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent list
              nitro agent list --role "backend"
              nitro agent list --stale
            """);
    }

    [Fact]
    public async Task NoAgents_PrintsEmptyMessage()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        result.AssertSuccess(
            """
            No registered agents.
            """);
    }

    [Fact]
    public async Task JsonOutput_NoAgents_ReturnsEmptyArray()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        result.AssertSuccess(
            """
            {
              "items": []
            }
            """);
    }

    [Fact]
    public async Task Agents_AreOrderedByName()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "zeta");
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await ExecuteCommandAsync("agent", "register", "--actor", "mu");

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        var lines = result.StdOut.Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("alpha", lines[0]);
        Assert.StartsWith("mu", lines[1]);
        Assert.StartsWith("zeta", lines[2]);
    }

    [Fact]
    public async Task JsonOutput_ReturnsRegisteredAgentsWithRole()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha", "--role", "backend");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        var items = root.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("alpha", items[0].GetProperty("name").GetString());
        Assert.Equal("backend", items[0].GetProperty("role").GetString());
        Assert.False(items[0].GetProperty("implicit").GetBoolean());
    }

    [Fact]
    public async Task RoleOption_FiltersByExactRole()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha", "--role", "backend");
        await ExecuteCommandAsync("agent", "register", "--actor", "beta", "--role", "frontend");

        // act
        var result = await ExecuteCommandAsync("agent", "list", "--role", "backend");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.StartsWith("alpha", line);
    }

    [Fact]
    public async Task StaleOption_FiltersByLastSeenOlderThan30Days()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "old-agent");
        FakeTime.Advance(TimeSpan.FromDays(31));
        await ExecuteCommandAsync("agent", "register", "--actor", "fresh-agent");

        // act
        var result = await ExecuteCommandAsync("agent", "list", "--stale");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.StartsWith("old-agent", line);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }
}
