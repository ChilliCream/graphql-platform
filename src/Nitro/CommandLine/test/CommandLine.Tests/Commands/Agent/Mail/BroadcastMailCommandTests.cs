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
              --body <body>                   The message body. Exactly one of --body or --body-file is required
              --body-file <body-file>         A file to read the message body from. Exactly one of --body or --body-file is required
              --role <role>                   The actor role, normalized lowercase
              --actor <actor>                 The actor performing this command; inferred from the current session when omitted
              --output <json>                 The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                  Show help and usage information

            Example:
              nitro agent mail broadcast --subject "Heads up" --body "Deploying at 5pm."
              nitro agent mail broadcast --role "backend" --subject "Heads up" --body "Deploying at 5pm."
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
            wake delivered.
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
            wake delivered.
            """);
    }

    [Fact]
    public async Task RoleFilter_SendsOnlyToLiveAgentsWithThatRole()
    {
        // arrange: durable registration alone is not enough - each candidate
        // needs a live session bound with that role, per the fix direction.
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
            wake delivered.
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
        // arrange: zeta once registered as orchestrator, but has no live
        // session at all now (the session ended, its row was reaped or
        // deleted) - a planner-style role lookup must not find it.
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
        // arrange: a session bound before role-aware registration (xy9.5)
        // never had its own role written, so discovery falls back to the
        // durable identity's role for it - but a closed identity with the
        // same durable role and no live session at all still is not found.
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
            wake delivered.
            """);
    }

    [Fact]
    public async Task RoleFilter_ExcludesAnImplicitIdentity_EvenWhenItsLiveSessionHasTheRole()
    {
        // arrange: an implicit identity (never registered itself) whose live
        // session was directly given a matching role - still excluded,
        // mirroring the plain broadcast's exclusion of implicit rows.
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
        // arrange: zeta has two live sessions both claiming orchestrator -
        // the broadcast must reach the actor once, not twice.
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
            wake delivered.
            """);
        Assert.Equal(
            "1",
            await QueryScalarAsync($"SELECT COUNT(*) FROM message_recipients WHERE message_id = '{id}'"));
    }

    [Fact]
    public async Task RoleFilter_ReflectsTheCurrentRole_AfterTheLiveSessionsRoleChanges()
    {
        // arrange: zeta's live session starts as backend, then its role
        // changes to orchestrator - discovery must follow the session's
        // current role, not the role it had when the row was created.
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
            wake delivered.
            """);
    }

    [Fact]
    public async Task RoleFilter_ExcludesAnUnboundSession()
    {
        // arrange: a role can only end up on a session together with a
        // binding through RegisterAsync, but discovery must not trust the
        // role column alone - it must also require the session be bound.
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
        // arrange: this pins MailRoleRecipients composed directly with
        // IMailStore.SendMessageAsync, not BroadcastMailCommand's own wiring
        // (its resolve-then-send has no interleaving point to race). Resolve
        // the role-targeted recipient, then end the session before the
        // durable send actually runs - discovery only feeds durable actor
        // names into the same async send path every mail command uses, so
        // the send must not depend on the row still existing.
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
            wake delivered.
            """);
        var call = Assert.Single(queueClient.Calls);
        Assert.Equal("thread-zeta", call.ThreadId);
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
        Assert.Equal("delivered", root.GetProperty("notification").GetProperty("status").GetString());
    }

    [Fact]
    public async Task JsonOutput_Should_ReturnCleanJson_And_ExitNonzero_When_TheRecipientHasNoLiveSession()
    {
        // arrange: two recipients, neither with a live claimed session, so
        // the direct-first dispatcher has nobody to address for either - the
        // message is durably stored but the wake is a confirmed failure for
        // both.
        await InitWorkspaceAsync();
        SetupInstanceId("host-broadcast-nolive-test");
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedAgentAsync("zeta");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "Heads up", "--body", "Deploying.");

        // assert: exactly one clean JSON object on stdout, nothing on
        // stderr, even though the command exits nonzero.
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        Assert.Equal(
            ["bob", "zeta"], root.GetProperty("to").EnumerateArray().Select(e => e.GetString()!).ToArray());
        Assert.True(root.GetProperty("messageStored").GetBoolean());
        var notification = root.GetProperty("notification");
        Assert.Equal("failed", notification.GetProperty("status").GetString());
        var recipients = notification.GetProperty("recipients").EnumerateArray().ToArray();
        Assert.Equal(2, recipients.Length);
        Assert.All(recipients, recipient => Assert.Equal("failed", recipient.GetProperty("status").GetString()));

        var pingResult = await QueryScalarAsync(
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-1'");
        Assert.Null(pingResult);
    }

    [Fact]
    public async Task JsonOutput_Should_ReportFailed_When_OneRecipientDeliversAndAnotherHasNoLiveSession()
    {
        // arrange: bob has a live claimed codex-thread session that
        // delivers, zeta has none at all - a mixed multi-recipient outcome
        // must control the command's own exit while every recipient's own
        // outcome remains visible in the receipt.
        await InitWorkspaceAsync();
        const string host = "host-broadcast-mixed-failure-test";
        SetupInstanceId(host);
        SetupCodexQueueClient(new FakeCodexQueueClient());
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedAgentAsync("zeta");
        await SeedAliveCodexThreadSessionAsync("bob", "thread-bob", host);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "Heads up", "--body", "Deploying.");

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        var notification = root.GetProperty("notification");
        Assert.Equal("failed", notification.GetProperty("status").GetString());
        var recipients = notification.GetProperty("recipients").EnumerateArray()
            .ToDictionary(recipient => recipient.GetProperty("actor").GetString()!);
        Assert.Equal("delivered", recipients["bob"].GetProperty("status").GetString());
        Assert.Equal("failed", recipients["zeta"].GetProperty("status").GetString());
    }

    [Fact]
    public async Task HumanOutput_Should_ReportDelivered_When_TheWakeReachesALiveSession()
    {
        // arrange: bob has a live claimed codex-thread session and the fake
        // codex queue client reports success, so the direct-first dispatcher
        // delivers the wake in the foreground.
        await InitWorkspaceAsync();
        const string host = "host-broadcast-delivered-human-test";
        SetupInstanceId(host);
        SetupCodexQueueClient(new FakeCodexQueueClient());
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await SeedAliveCodexThreadSessionAsync("bob", "thread-bob", host);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "Heads up", "--body", "Deploying.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Heads up'");
        result.AssertSuccess(
            $"""
            ✓ Sent '{id}' to bob.
            wake delivered.
            """);
    }

    [Fact]
    public async Task HumanOutput_Should_ListEveryFailingRecipient_When_MixedResultsFail()
    {
        // arrange: same mixed scenario as the JSON failure test, asserting
        // the human-readable rendering instead.
        await InitWorkspaceAsync();
        const string host = "host-broadcast-mixed-failure-human-test";
        SetupInstanceId(host);
        SetupCodexQueueClient(new FakeCodexQueueClient());
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedAgentAsync("zeta");
        await SeedAliveCodexThreadSessionAsync("bob", "thread-bob", host);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "Heads up", "--body", "Deploying.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Heads up'");
        result.AssertError(
            $"""
            Stored '{id}' to bob, zeta.
            message stored, but wake failed: no-live-session.
              zeta: failed (no-live-session)
            """);
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
