namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class ListAgentCommandTests : AgentCommandTestBase
{
    private const string FixedHost = "host-list-agent-tests";

    public ListAgentCommandTests(NitroCommandFixture fixture) : base(fixture)
    {
        SetupInstanceId(FixedHost);
    }

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
        await ExecuteCommandAsync(
            "agent", "register", "--actor", "alpha", "--role", "backend", "--client", "claude-code");
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
        Assert.Equal("claude-code", items[0].GetProperty("client").GetString());
        Assert.False(items[0].GetProperty("implicit").GetBoolean());
    }

    [Fact]
    public async Task HumanReadableOutput_ShowsClientSuffix_When_ClientIsSet()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha", "--client", "claude-code");

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("client claude-code", line);
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

    [Fact]
    public async Task HumanReadableOutput_ShowsOffline_When_AgentHasNoLiveSession()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("alpha  offline", line);
    }

    [Fact]
    public async Task HumanReadableOutput_ShowsOnline_When_AgentHasALiveSessionWithAnEndpoint()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await InsertAliveSessionRowAsync(
            FixedHost, "session-1", "alpha", endpointKind: "claude-peer", endpointAddr: "alpha-peer");

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("alpha  online", line);
    }

    [Fact]
    public async Task HumanReadableOutput_ShowsUnreachable_When_AgentHasALiveSessionWithNoEndpoint()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await InsertAliveSessionRowAsync(FixedHost, "session-1", "alpha");

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("alpha  unreachable", line);
    }

    [Fact]
    public async Task HumanReadableOutput_ShowsOffline_When_TheOnlySessionRowIsADeadGeneration()
    {
        // arrange: a dead-generation row on the current instance is reaped
        // on read, not reported as some stale presence state.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await InsertDeadSessionRowAsync(FixedHost, "session-dead", "alpha");

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("alpha  offline", line);
    }

    [Fact]
    public async Task HumanReadableOutput_ShowsRemote_When_AgentsLiveSessionIsOnAnotherInstance()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await InsertAliveSessionRowAsync("some-other-host", "session-1", "alpha");

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("alpha  remote", line);
    }

    [Fact]
    public async Task HumanReadableOutput_SurfacesConflict_When_SameActorSessionsDisagreeOnState()
    {
        // arrange: a same-actor restart leaves two live sessions - one
        // online, one unreachable - and the plan requires that disagreement
        // is surfaced, not silently collapsed into one of the two states.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await InsertAliveSessionRowAsync(
            FixedHost, "session-1", "alpha", endpointKind: "claude-peer", endpointAddr: "alpha-peer");
        await InsertAliveSessionRowAsync(FixedHost, "session-2", "alpha", harness: "codex");

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert: both states are named, and the session count is shown so
        // the conflict cannot be mistaken for either state alone.
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("online+unreachable", line);
        Assert.Contains("(2 sessions)", line);
    }

    [Fact]
    public async Task JsonOutput_IncludesPresenceAndEndpointColumns()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await InsertAliveSessionRowAsync(
            FixedHost, "session-1", "alpha", endpointKind: "claude-peer", endpointAddr: "alpha-peer");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var item = document.RootElement.GetProperty("items")[0];

        Assert.Equal("online", item.GetProperty("presence").GetString());
        Assert.False(item.GetProperty("presenceConflict").GetBoolean());
        Assert.Equal(1, item.GetProperty("sessionCount").GetInt32());
        Assert.Equal("claude-peer", item.GetProperty("endpointKind").GetString());
        Assert.Equal("alpha-peer", item.GetProperty("endpointAddr").GetString());
    }

    [Fact]
    public async Task JsonOutput_OmitsEndpointColumns_When_AgentHasNoLiveSession()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var item = document.RootElement.GetProperty("items")[0];

        Assert.Equal("offline", item.GetProperty("presence").GetString());
        Assert.Equal(0, item.GetProperty("sessionCount").GetInt32());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, item.GetProperty("endpointKind").ValueKind);
    }
}
