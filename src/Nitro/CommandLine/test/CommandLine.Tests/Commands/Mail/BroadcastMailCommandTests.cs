using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

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
              --role <role>                   The agent's role, free text, normalized lowercase (defaults to empty)
              --actor <actor>                 The acting identity used on mail commands (defaults to NITRO_MAIL_ACTOR, NITRO_TASK_ACTOR, or the OS user name)
              --no-ping                       Skip the best-effort wake ping to recipients with a live claimed session
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

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "Heads up", "--body", "Deploying.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Heads up'");
        result.AssertSuccess($"✓ Sent '{id}' to alpha, zeta.");
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
        await ExecuteCommandAsync(
            "agent", "mail", "send", "implicit-agent", "--actor", "test-agent",
            "--subject", "seed", "--body", "seed");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "Heads up", "--body", "Deploying.");

        // assert
        var id = await QueryScalarAsync(
            "SELECT id FROM messages WHERE subject = 'Heads up'");
        result.AssertSuccess($"✓ Sent '{id}' to zeta.");
    }

    [Fact]
    public async Task RoleFilter_SendsOnlyToLiveAgentsWithThatRole()
    {
        // arrange: durable registration alone is not enough - each candidate
        // needs a live session bound with that role, per the fix direction.
        await InitWorkspaceAsync();
        SetupInstanceId("host-broadcast-role-test");
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "zeta");
        await ExecuteCommandAsync("agent", "register", "--actor", "alpha");
        await SeedAliveSessionAsync("session-zeta", "zeta", "backend", "host-broadcast-role-test");
        await SeedAliveSessionAsync("session-alpha", "alpha", "frontend", "host-broadcast-role-test");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--role", "backend",
            "--subject", "Heads up", "--body", "Deploying.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Heads up'");
        result.AssertSuccess($"✓ Sent '{id}' to zeta.");
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
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync(
            "agent", "register", "--actor", "zeta", "--role", "orchestrator");

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
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync(
            "agent", "register", "--actor", "zeta", "--role", "orchestrator");
        await SeedAliveSessionAsync(
            "session-zeta", "zeta", role: "", host: "host-broadcast-fallback-test");
        await ExecuteCommandAsync(
            "agent", "register", "--actor", "closed-orchestrator", "--role", "orchestrator");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--role", "orchestrator",
            "--subject", "Heads up", "--body", "Deploying.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Heads up'");
        result.AssertSuccess($"✓ Sent '{id}' to zeta.");
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
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "zeta");
        await SeedAliveSessionAsync("session-zeta-1", "zeta", "orchestrator", "host-broadcast-dedup-test");
        await SeedAliveSessionAsync("session-zeta-2", "zeta", "orchestrator", "host-broadcast-dedup-test");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--role", "orchestrator",
            "--subject", "Heads up", "--body", "Deploying.");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Heads up'");
        result.AssertSuccess($"✓ Sent '{id}' to zeta.");
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
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "zeta");
        await SeedAliveSessionAsync("session-zeta", "zeta", "backend", "host-broadcast-rolechange-test");
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
        orchestratorResult.AssertSuccess($"✓ Sent '{id}' to zeta.");
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
    public async Task RoleFilter_ResolvesTheLiveSession_And_LeavesItUntouched_When_NoWakeGenerationIsOutstanding()
    {
        // arrange: proves notifier fan-out still fires for a recipient
        // resolved through the role-targeted, live-participant path, and
        // stays clean since broadcast still defaults to MailWakePolicy.Skip.
        await InitWorkspaceAsync();
        SetupInstanceId("host-broadcast-role-ping-test");
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
        result.AssertSuccess($"✓ Sent '{id}' to zeta.");
        var pingResult = await QueryScalarAsync(
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-zeta'");
        Assert.Null(pingResult);
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
    }

    [Fact]
    public async Task JsonOutput_Should_ReturnCleanJson_When_TheRecipientHasNoOutstandingWakeGeneration()
    {
        // arrange: two recipients with live claimed codex-thread sessions,
        // but broadcast still defaults to MailWakePolicy.Skip, so the
        // direct-first dispatcher finds nothing outstanding and never
        // touches the row.
        await InitWorkspaceAsync();
        SetupInstanceId("host-broadcast-test");
        await ExecuteCommandAsync("agent", "register", "--actor", "test-agent");
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await ExecuteCommandAsync("agent", "register", "--actor", "zeta");
        await SeedAliveCodexThreadSessionAsync("bob", "thread-bob", "host-broadcast-test");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "broadcast", "--subject", "Heads up", "--body", "Deploying.");

        // assert: the notifier never alters mail's own exit code or stdout -
        // a single clean JSON result, nothing else.
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        Assert.Equal(
            ["bob", "zeta"], root.GetProperty("to").EnumerateArray().Select(e => e.GetString()!).ToArray());

        var pingResult = await QueryScalarAsync(
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-1'");
        Assert.Null(pingResult);
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
