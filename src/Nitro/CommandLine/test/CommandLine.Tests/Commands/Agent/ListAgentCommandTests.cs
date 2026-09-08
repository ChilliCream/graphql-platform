using ChilliCream.Nitro.CommandLine.Services.Workspace;

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
        var result = await ExecuteCommandAsync("agent", "list", "--help");

        result.AssertHelpOutput(
            """
            Description:
              List the actors this workspace knows, with their session when they have one.

            Usage:
              nitro agent list [options]

            Options:
              --role <role>    The actor role, normalized lowercase. Known roles: orchestrator, planner, implementer, reviewer, researcher; any other value is accepted.
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent list
              nitro agent list --role "orchestrator"
            """);
    }

    [Fact]
    public async Task Execute_Should_PrintNoActors_When_NoIdentitiesExist()
    {
        await InitWorkspaceAsync();

        var result = await ExecuteCommandAsync("agent", "list");

        result.AssertSuccess("No actors.");
    }

    [Fact]
    public async Task Execute_Should_ListTheActor_When_ItHasNoSession()
    {
        await InitWorkspaceAsync();
        await SeedAgentAsync("alpha");

        var result = await ExecuteCommandAsync("agent", "list");

        result.AssertSuccess("alpha  offline  no session  last heard 2026-01-01 00:00");
    }

    [Fact]
    public async Task Execute_Should_ListIdentityAsOffline_When_NoConnectionExists()
    {
        await InitWorkspaceAsync();
        await InsertSessionIdentityAsync("maya", "session-1", role: "planner");

        var result = await ExecuteCommandAsync("agent", "list");

        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.StartsWith("maya  offline  claude-code  role planner", line);
    }

    [Fact]
    public async Task Execute_Should_IgnoreConnection_When_ItHasNoIdentity()
    {
        await InitWorkspaceAsync();
        await InsertAliveSessionRowAsync(FixedHost, "session-1", agentName: null, bindingKind: "none");

        var result = await ExecuteCommandAsync("agent", "list");

        result.AssertSuccess("No actors.");
    }

    [Fact]
    public async Task Execute_Should_ShowOnlineStateAndVersion_When_LiveConnectionExists()
    {
        await InitWorkspaceAsync();
        await InsertAliveSessionRowAsync(
            FixedHost,
            "session-1",
            "maya",
            endpointKind: AgentSessionEndpointKind.ClaudePeer,
            endpointAddr: "maya-peer",
            harnessVersion: "2.1.241");
        await InsertSessionIdentityAsync("maya", "session-1");

        var result = await ExecuteCommandAsync("agent", "list");

        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.StartsWith("maya  online  claude-code 2.1.241", line);
    }

    [Fact]
    public async Task Execute_Should_ShowRemoteState_When_ConnectionBelongsToAnotherHost()
    {
        await InitWorkspaceAsync();
        await InsertAliveSessionRowAsync("other-host", "session-1", "maya");
        await InsertSessionIdentityAsync("maya", "session-1");

        var result = await ExecuteCommandAsync("agent", "list");

        Assert.Contains("maya  remote", result.StdOut);
    }

    [Fact]
    public async Task Execute_Should_KeepIdentityOffline_When_StaleConnectionIsReaped()
    {
        await InitWorkspaceAsync();
        await InsertStaleSessionRowAsync(FixedHost, "session-1", "maya");
        await InsertSessionIdentityAsync("maya", "session-1");

        var result = await ExecuteCommandAsync("agent", "list");

        Assert.Contains("maya  offline", result.StdOut);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_sessions"));
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM agent_session_identities"));
    }

    [Fact]
    public async Task Execute_Should_FilterByIdentityRole_When_IdentityIsOffline()
    {
        await InitWorkspaceAsync();
        await InsertSessionIdentityAsync("maya", "session-1", role: "orchestrator");
        await InsertSessionIdentityAsync(
            "nova", "session-2", harness: AgentSessionHarness.Codex, role: "planner");

        var result = await ExecuteCommandAsync("agent", "list", "--role", "orchestrator");

        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.StartsWith("maya  offline", line);
    }

    [Fact]
    public async Task Execute_Should_OmitConnectionDiagnostics_When_IdentityIsOffline()
    {
        await InitWorkspaceAsync();
        await InsertSessionIdentityAsync("maya", "session-offline", role: "planner");
        SetupInteractionMode(InteractionMode.JsonOutput);

        var result = await ExecuteCommandAsync("agent", "list");

        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var item = Assert.Single(document.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal("maya", item.GetProperty("actor").GetString());
        Assert.Equal("session-offline", item.GetProperty("sessionId").GetString());
        Assert.False(item.GetProperty("online").GetBoolean());
        Assert.Equal("offline", item.GetProperty("state").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, item.GetProperty("host").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, item.GetProperty("endpointKind").ValueKind);
    }

    [Fact]
    public async Task Execute_Should_IncludeConnectionDiagnostics_When_IdentityIsOnline()
    {
        await InitWorkspaceAsync();
        await InsertAliveSessionRowAsync(
            FixedHost,
            "session-online",
            "maya",
            endpointKind: AgentSessionEndpointKind.ClaudePeer,
            endpointAddr: "maya-peer");
        await InsertSessionIdentityAsync("maya", "session-online");
        SetupInteractionMode(InteractionMode.JsonOutput);

        var result = await ExecuteCommandAsync("agent", "list");

        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var item = Assert.Single(document.RootElement.GetProperty("items").EnumerateArray());
        Assert.True(item.GetProperty("online").GetBoolean());
        Assert.Equal(FixedHost, item.GetProperty("host").GetString());
        Assert.Equal("maya-peer", item.GetProperty("endpointAddr").GetString());
    }
}
