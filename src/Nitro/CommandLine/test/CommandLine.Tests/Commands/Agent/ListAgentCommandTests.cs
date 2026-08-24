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
              List live agent participants: one row per harness session, including unbound sessions.

            Usage:
              nitro agent list [options]

            Options:
              --role <role>    The agent's role, free text, normalized lowercase (defaults to empty)
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent list
              nitro agent list --role "orchestrator"
            """);
    }

    [Fact]
    public async Task NoSessions_PrintsEmptyMessage()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        result.AssertSuccess(
            """
            No live agent participants.
            """);
    }

    [Fact]
    public async Task JsonOutput_NoSessions_ReturnsEmptyArray()
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
    public async Task HumanReadableOutput_ShowsNothing_When_AnActorHasHistoryButNoLiveSession()
    {
        // arrange: a durable identity that has registered before, but has
        // no live harness session right now. The old durable-agents-table
        // listing would still show it (stale historical identity); the
        // live-participant listing must not.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        result.AssertSuccess(
            """
            No live agent participants.
            """);
    }

    [Fact]
    public async Task HumanReadableOutput_ShowsUnboundSession_WithNoActor()
    {
        // arrange: a live harness session that has never been claimed or
        // registered by any actor.
        await InitWorkspaceAsync();
        await InsertAliveSessionRowAsync(FixedHost, "session-1", agentName: null, bindingKind: "none");

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.StartsWith("unbound", line);
    }

    [Fact]
    public async Task JsonOutput_ShowsUnboundSession_WithNullActor()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertAliveSessionRowAsync(FixedHost, "session-1", agentName: null, bindingKind: "none");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var item = document.RootElement.GetProperty("items")[0];

        Assert.Equal(System.Text.Json.JsonValueKind.Null, item.GetProperty("actor").ValueKind);
        Assert.Equal("session-1", item.GetProperty("sessionId").GetString());
    }

    [Fact]
    public async Task Rows_Should_ShowTwoSeparateRows_When_TwoSessionsShareOneActor()
    {
        // arrange: a same-actor restart leaves two live sessions bound to
        // the same actor; the live-participant listing never aggregates
        // them into one row.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await InsertAliveSessionRowAsync(FixedHost, "session-1", "alpha");
        await InsertAliveSessionRowAsync(FixedHost, "session-2", "alpha", harness: "codex");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items");

        Assert.Equal(2, items.GetArrayLength());
        Assert.All(items.EnumerateArray(), item => Assert.Equal("alpha", item.GetProperty("actor").GetString()));
    }

    [Fact]
    public async Task Refresh_Should_ShowPromotedRole_WithoutDuplicatingTheRow()
    {
        // arrange: a session's mutable role changes on the SAME (harness,
        // session id) row (the shape IAgentSessionRegistry.RegisterAsync
        // applies); the listing must show exactly one row, with the new
        // role, never a second row for the promotion.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await InsertAliveSessionRowAsync(FixedHost, "session-1", "alpha", role: "");
        await UpdateSessionRoleAsync(FixedHost, "session-1", "orchestrator");

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("role orchestrator", line);
    }

    [Fact]
    public async Task RoleOption_FiltersByTheMutableSessionRole_NotTheDurableIdentityRole()
    {
        // arrange: planners locate the orchestrator by --role, which must
        // match a bound live row's mutable session role.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await ExecuteCommandAsync("agent", "register", "--actor", "beta");
        await InsertAliveSessionRowAsync(FixedHost, "session-1", "alpha", role: "orchestrator");
        await InsertAliveSessionRowAsync(FixedHost, "session-2", "beta", role: "planner");

        // act
        var result = await ExecuteCommandAsync("agent", "list", "--role", "orchestrator");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.StartsWith("alpha", line);
    }

    [Fact]
    public async Task HumanReadableOutput_ShowsHarnessAndExactVersion()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await InsertAliveSessionRowAsync(
            FixedHost, "session-1", "alpha", harness: "claude-code", harnessVersion: "2.1.241");

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("claude-code 2.1.241", line);
    }

    [Fact]
    public async Task HumanReadableOutput_ShowsState()
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
        Assert.Contains("online", line);
    }

    [Fact]
    public async Task HumanReadableOutput_ShowsUnreachable_When_TheSessionHasNoEndpoint()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await InsertAliveSessionRowAsync(FixedHost, "session-1", "alpha");

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("unreachable", line);
    }

    [Fact]
    public async Task HumanReadableOutput_ShowsRemote_When_TheSessionIsOnAnotherInstance()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await InsertAliveSessionRowAsync("some-other-host", "session-1", "alpha");

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("remote", line);
    }

    [Fact]
    public async Task HumanReadableOutput_OmitsDeadGenerationRow()
    {
        // arrange: a dead-generation row on the current instance is reaped
        // on read, not reported at all.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await InsertDeadSessionRowAsync(FixedHost, "session-dead", "alpha");

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        result.AssertSuccess(
            """
            No live agent participants.
            """);
    }

    [Fact]
    public async Task JsonOutput_IncludesFullSessionIdAndDiagnosticColumns()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await InsertAliveSessionRowAsync(
            FixedHost, "session-1-full-id", "alpha", endpointKind: "claude-peer", endpointAddr: "alpha-peer");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var item = document.RootElement.GetProperty("items")[0];

        Assert.Equal("claude-code", item.GetProperty("harness").GetString());
        Assert.Equal("session-1-full-id", item.GetProperty("sessionId").GetString());
        Assert.Equal("alpha", item.GetProperty("actor").GetString());
        Assert.Equal("online", item.GetProperty("state").GetString());
        Assert.Equal("/work", item.GetProperty("cwd").GetString());
        Assert.Equal("/work/.nitro/agents", item.GetProperty("workspacePath").GetString());
        Assert.Equal(FixedHost, item.GetProperty("host").GetString());
        Assert.Equal("claude-peer", item.GetProperty("endpointKind").GetString());
        Assert.Equal("alpha-peer", item.GetProperty("endpointAddr").GetString());
    }
}
