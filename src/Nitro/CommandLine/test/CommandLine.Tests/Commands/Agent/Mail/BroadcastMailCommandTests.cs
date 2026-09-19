using ChilliCream.Nitro.CommandLine.Services.Mail;
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
    public async Task ExcludesImplicitRows_SendsOnlyToRegistered()
    {
        // arrange
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
            ✓ Sent '{id}' to zeta.
            """);
    }

    [Fact]
    public async Task RoleFilter_SendsOnlyToLiveAgentsWithThatRole()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInstanceId("host-broadcast-role-test");
        SetupCodexQueueClient(new FakeCodexQueueClient());
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "zeta");
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await SeedAliveSessionAsync(
            "session-zeta", "zeta", "backend", "host-broadcast-role-test",
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: "thread-zeta");
        await SeedAliveSessionAsync("session-alpha", "alpha", "frontend", "host-broadcast-role-test");

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
        SetupInstanceId("host-broadcast-role-test");
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "zeta");
        await SeedAliveSessionAsync("session-zeta", "zeta", "frontend", "host-broadcast-role-test");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--role", "backend",
            "--subject", "hi", "--body", "hello");

        // assert
        result.AssertError(
            """
            No live agent with role 'backend' to broadcast to (older sessions must re-register).
            """);
    }

    [Fact]
    public async Task RoleFilter_ExcludesClosedHistoricalIdentity_ReturnsNoLiveRecipientError()
    {
        // arrange
        // Seed a registered orchestrator identity without a session.
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("zeta", "orchestrator");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--role", "orchestrator",
            "--subject", "hi", "--body", "hello");

        // assert
        result.AssertError(
            """
            No live agent with role 'orchestrator' to broadcast to (older sessions must re-register).
            """);
    }

    [Fact]
    public async Task RoleFilter_FallsBackToTheDurableRole_When_TheLiveSessionsOwnRoleIsBlank()
    {
        // arrange
        // Only zeta has a live session, with a blank role and an orchestrator identity.
        await InitWorkspaceAsync();
        SetupInstanceId("host-broadcast-fallback-test");
        SetupCodexQueueClient(new FakeCodexQueueClient());
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("zeta", "orchestrator");
        await SeedAliveSessionAsync(
            "session-zeta", "zeta", role: "", host: "host-broadcast-fallback-test",
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: "thread-zeta");
        await SeedAgentAsync("closed-orchestrator", "orchestrator");

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
    public async Task RoleFilter_ExcludesAnImplicitIdentity_EvenWhenItsLiveSessionHasTheRole()
    {
        // arrange
        // Seed an implicit identity with a live backend session.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync();
        SetupInstanceId("host-broadcast-implicit-test");
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await CreateRegistry().EnsureImplicitAsync("ghost", cancellationToken);
        await SeedAliveSessionAsync("session-ghost", "ghost", "backend", "host-broadcast-implicit-test");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--role", "backend",
            "--subject", "hi", "--body", "hello");

        // assert
        result.AssertError(
            """
            No live agent with role 'backend' to broadcast to (older sessions must re-register).
            """);
    }

    [Fact]
    public async Task RoleFilter_DedupesMultipleLiveSessionsForTheSameActor()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInstanceId("host-broadcast-dedup-test");
        SetupCodexQueueClient(new FakeCodexQueueClient());
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "zeta");
        await SeedAliveSessionAsync(
            "session-zeta-1", "zeta", "orchestrator", "host-broadcast-dedup-test",
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: "thread-zeta-1");
        await SeedAliveSessionAsync(
            "session-zeta-2", "zeta", "orchestrator", "host-broadcast-dedup-test",
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: "thread-zeta-2");

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
        Assert.Equal(
            "1",
            await QueryScalarAsync($"SELECT COUNT(*) FROM message_recipients WHERE message_id = '{id}'"));
    }

    [Fact]
    public async Task RoleFilter_ReflectsTheCurrentRole_AfterTheLiveSessionsRoleChanges()
    {
        // arrange
        // Change the session role from backend to orchestrator before discovery.
        await InitWorkspaceAsync();
        SetupInstanceId("host-broadcast-rolechange-test");
        SetupCodexQueueClient(new FakeCodexQueueClient());
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "zeta");
        await SeedAliveSessionAsync(
            "session-zeta", "zeta", "backend", "host-broadcast-rolechange-test",
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: "thread-zeta");
        await ExecuteAsync("UPDATE agent_sessions SET role = 'orchestrator' WHERE session_id = 'session-zeta'");

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
            No live agent with role 'backend' to broadcast to (older sessions must re-register).
            """);
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'orchestrator broadcast'");
        orchestratorResult.AssertSuccess(
            $"""
            ✓ Sent '{id}' to zeta.
            """);
    }

    [Fact]
    public async Task RoleFilter_ExcludesAnUnboundSession()
    {
        // arrange
        // Seed a session with an orchestrator role and no bound agent.
        await InitWorkspaceAsync();
        SetupInstanceId("host-broadcast-unbound-test");
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await SeedAliveSessionAsync(
            "session-unbound", agentName: null, role: "orchestrator", host: "host-broadcast-unbound-test");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--role", "orchestrator",
            "--subject", "hi", "--body", "hello");

        // assert
        result.AssertError(
            """
            No live agent with role 'orchestrator' to broadcast to (older sessions must re-register).
            """);
    }

    [Fact]
    public async Task MailRoleRecipients_ResolvedRecipient_StillDeliversDurably_When_TheSessionEndsBeforeSend()
    {
        // arrange
        // Resolve the recipient before deleting its session and sending through the mail store.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("zeta");
        await SeedAliveSessionAsync("session-zeta", "zeta", "orchestrator", "host-broadcast-race-test");

        var to = await MailRoleRecipients.ResolveAsync(
            CreateSessions("host-broadcast-race-test"), "orchestrator", "test-agent", cancellationToken);

        // act
        await ExecuteAsync("DELETE FROM agent_sessions WHERE session_id = 'session-zeta'");
        var message = await CreateStore().SendMessageAsync(
            new MailMessageCreation
            {
                Sender = "test-agent",
                Subject = "Heads up",
                Body = "Deploying.",
                To = to
            },
            cancellationToken);

        // assert
        Assert.Equal(["zeta"], to);
        Assert.Equal(["zeta"], message.Recipients.Select(recipient => recipient.Name));
    }

    [Fact]
    public async Task RoleFilter_Should_WakeResolvedLiveSession_When_RoleMatches()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInstanceId("host-broadcast-role-ping-test");
        var queueClient = new FakeCodexQueueClient();
        SetupCodexQueueClient(queueClient);
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "zeta");
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
