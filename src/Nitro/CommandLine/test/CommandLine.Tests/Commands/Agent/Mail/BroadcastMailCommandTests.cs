using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Mail;

public sealed class BroadcastMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "broadcast", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Send a message to every registered agent except yourself.

            Usage:
              nitro agent mail broadcast [options]

            Options:
              --subject <subject> (REQUIRED)  The message subject
              --body <body>                   The message body; use --body-file to read it from a file instead
              --body-file <body-file>         A file to read the message body from; use it instead of --body
              --role <role>                   The actor role, normalized lowercase. Known roles: orchestrator, planner, implementer, reviewer, researcher; any other value is accepted.
              --actor <actor> (REQUIRED)      The actor performing this command; allocate one with `nitro agent login`
              --output <json>                 The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                  Show help and usage information

            Example:
              nitro agent mail broadcast --subject "Heads up" --body "Deploying at 5pm." --actor "maya"
              nitro agent mail broadcast --role "backend" --subject "Heads up" --body "Deploying at 5pm." --actor "maya"
            """);
    }

    [Fact]
    public async Task ExcludesSender_SendsToOthersOrderedByName()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "zeta");
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await SetupSuccessfulWakeAsync("host-broadcast-order-test", "alpha", "zeta");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "Heads up", "--body", "Deploying.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Heads up'");
        result.AssertSuccess(
            $"""
            ✓ Sent '{id}' to alpha, zeta.
            """);
    }

    [Fact]
    public async Task NoOtherRegisteredAgent_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "solo");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "hi", "--body", "hello", "--actor", "solo");

        // assert
        result.AssertError(
            """
            No other registered agent to broadcast to.
            """);
    }

    [Fact]
    public async Task IncludesLegacyImplicitRows_SendsToEveryNonDeletedAgent()
    {
        // arrange
        // The legacy implicit flag no longer gates broadcast recipients.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "zeta");
        await CreateRegistry().EnsureImplicitAsync("implicit-agent", TestContext.Current.CancellationToken);
        await SetupSuccessfulWakeAsync("host-broadcast-implicit-row-test", "zeta");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "Heads up", "--body", "Deploying.");

        // assert
        var id = await QueryScalarAsync(
            "SELECT id FROM messages WHERE subject = 'Heads up'");
        result.AssertSuccess(
            $"""
            ✓ Sent '{id}' to implicit-agent, zeta.
            """);
    }

    [Fact]
    public async Task ExcludesDeletedAgent_SendsOnlyToNonDeleted()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "zeta");
        await SeedAgentAsync("gone");
        await MarkAgentDeletedAsync("gone");
        await SetupSuccessfulWakeAsync("host-broadcast-deleted-row-test", "zeta");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "Heads up", "--body", "Deploying.");

        // assert
        var id = await QueryScalarAsync(
            "SELECT id FROM messages WHERE subject = 'Heads up'");
        result.AssertSuccess(
            $"""
            ✓ Sent '{id}' to zeta.
            """);
    }

    [Fact]
    public async Task RoleFilter_SendsOnlyToAgentsWithThatRole()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("zeta", "backend");
        await SeedAgentAsync("alpha", "frontend");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--role", "backend",
            "--subject", "Heads up", "--body", "Deploying.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Heads up'");
        result.AssertSuccess(
            $"""
            ✓ Sent '{id}' to zeta.
            """);
    }

    [Fact]
    public async Task RoleFilter_NoMatchingAgent_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("zeta", "frontend");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--role", "backend",
            "--subject", "hi", "--body", "hello");

        // assert
        result.AssertError(
            """
            No agent with role 'backend' to broadcast to.
            """);
    }

    [Fact]
    public async Task RoleFilter_IncludesAgentWithoutALiveSession()
    {
        // arrange
        // Role now comes solely from the durable agents row; no session is required.
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("zeta", "orchestrator");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--role", "orchestrator",
            "--subject", "Heads up", "--body", "Deploying.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Heads up'");
        result.AssertSuccess(
            $"""
            ✓ Sent '{id}' to zeta.
            """);
    }

    [Fact]
    public async Task RoleFilter_ExcludesADeletedAgent_EvenWhenItsRoleMatches()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("ghost", "backend");
        await MarkAgentDeletedAsync("ghost");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--role", "backend",
            "--subject", "hi", "--body", "hello");

        // assert
        result.AssertError(
            """
            No agent with role 'backend' to broadcast to.
            """);
    }

    [Fact]
    public async Task RoleFilter_ReflectsTheCurrentRole_AfterItChanges()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("zeta", "backend");
        await ExecuteAsync("UPDATE agents SET role = 'orchestrator' WHERE name = 'zeta'");

        // act
        var backendResult = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--role", "backend",
            "--subject", "backend broadcast", "--body", "Deploying.");
        var orchestratorResult = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--role", "orchestrator",
            "--subject", "orchestrator broadcast", "--body", "Deploying.");

        // assert
        backendResult.AssertError(
            """
            No agent with role 'backend' to broadcast to.
            """);
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'orchestrator broadcast'");
        orchestratorResult.AssertSuccess(
            $"""
            ✓ Sent '{id}' to zeta.
            """);
    }

    [Fact]
    public async Task RoleFilter_Should_WakeLiveSession_When_RoleMatches()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInstanceId("host-broadcast-role-ping-test");
        var queueClient = new FakeCodexQueueClient();
        SetupCodexQueueClient(queueClient);
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("zeta", "orchestrator");
        await SeedAliveSessionAsync(
            "session-zeta", "zeta", "orchestrator", "host-broadcast-role-ping-test",
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: "thread-zeta");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--role", "orchestrator",
            "--subject", "Heads up", "--body", "Deploying.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Heads up'");
        result.AssertSuccess(
            $"""
            ✓ Sent '{id}' to zeta.
            """);
        var call = Assert.Single(queueClient.Calls);
        Assert.Equal(("thread-zeta", id, "Deploying."), ReadDigestCall(call));
    }

    [Fact]
    public async Task NoRegisteredAgentsAtAll_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "hi", "--body", "hello");

        // assert
        result.AssertError(
            """
            No other registered agent to broadcast to.
            """);
    }

    [Fact]
    public async Task JsonOutput_ReturnsMessageResult()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SetupSuccessfulWakeAsync("host-broadcast-json-test", "bob");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "Heads up", "--body", "Deploying.");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("test-agent", root.GetProperty("from").GetString());
        Assert.Equal(["bob"], root.GetProperty("to").EnumerateArray().Select(e => e.GetString()!).ToArray());
        Assert.True(root.GetProperty("messageStored").GetBoolean());
    }

    [Fact]
    public async Task BodyAndBodyFileBothMissing_ReturnsParseError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "hi");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Exactly one of '--body' or '--body-file' is required.", result.StdErr);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "hi", "--body", "hello");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }
}
