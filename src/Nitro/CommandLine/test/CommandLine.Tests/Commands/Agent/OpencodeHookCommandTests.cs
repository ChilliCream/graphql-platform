using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers the command's wiring and the response it writes to stdout. The
/// session lifecycle, digest, and idle-reservation behavior are exercised
/// directly against <see cref="Services.Hook.OpencodeHookHandler"/> in
/// <c>OpencodeHookHandlerTests</c>, and the fail-open envelope against
/// <see cref="Services.Hook.OpencodeHookExecutor"/> in
/// <c>OpencodeHookExecutorTests</c>.
/// </summary>
public sealed class OpencodeHookCommandTests(NitroCommandFixture fixture) : AgentCommandTestBase(fixture)
{
    private const string SessionId = "session-1";
    private const string ServerUrl = "http://127.0.0.1:4096";

    [Fact]
    public async Task SessionCreated_Should_RegisterThePresenceRow_And_WriteNeutralResponse()
    {
        // arrange: an identity already bound to this session id, so the
        // registered row binds to the seeded name rather than an allocated one.
        await InitWorkspaceAsync();
        await InsertOpencodeIdentityAsync("maya");
        SetupHookPayload();

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "opencode", "session-created");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("maya", await QueryScalarAsync("SELECT agent_name FROM agent_sessions"));
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task SessionCreated_Should_WriteNeutralResponse_When_ThePayloadNamesNoServerUrl()
    {
        // arrange: a payload with no server URL, so nothing identifies the
        // session's opencode server endpoint.
        await InitWorkspaceAsync();
        await InsertOpencodeIdentityAsync("maya");
        SetupStandardInput(
            $$"""{"sessionId":"{{SessionId}}","cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}}}""");

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "opencode", "session-created");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_sessions"));
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task ChatMessage_Should_AppendTheActorAnnouncement_When_ItIsTheFirstMessage()
    {
        // arrange: session-created armed the first-message announcement, so
        // the first chat-message event claims it.
        await InitWorkspaceAsync();
        await InsertOpencodeIdentityAsync("maya");
        SetupHookPayload();
        await ExecuteCommandAsync("agent", "hook", "opencode", "session-created");
        SetupHookPayload();

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "opencode", "chat-message");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().MatchInlineSnapshot(
            "{\"parts\":[\"Your Nitro actor name is \\u0022maya\\u0022. Pass this name to the \\u0060--actor\\u0060 "
                + "option to act under this actor explicitly.\"]}");
    }

    [Fact]
    public async Task ChatMessage_Should_AppendTheMailDigest_When_TheActorHasUnreadMail()
    {
        // arrange: the announcement already claimed on session-created, and
        // one unread message sent to the bound actor afterward.
        await InitWorkspaceAsync();
        await InsertOpencodeIdentityAsync("maya");
        await SeedAgentAsync("ada");
        SetupHookPayload();
        await ExecuteCommandAsync("agent", "hook", "opencode", "session-created");
        SetupHookPayload();
        await ExecuteCommandAsync("agent", "hook", "opencode", "chat-message");
        await ExecuteCommandAsync(
            "agent", "mail", "send", "--body", "All good.", "--to", "maya", "--subject", "Status", "--actor", "ada");
        SetupHookPayload();

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "opencode", "chat-message");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().MatchInlineSnapshot(
            "{\"parts\":[\"You have 1 unread nitro message. Run \\u0060nitro agent mail inbox "
                + "--actor maya\\u0060.\"]}");
    }

    [Fact]
    public async Task ChatMessage_Should_WriteNeutralResponse_When_NoMailIsUnread_And_AnnouncementAlreadyClaimed()
    {
        // arrange: the announcement already claimed and no mail waiting, so
        // this event has nothing left to say.
        await InitWorkspaceAsync();
        await InsertOpencodeIdentityAsync("maya");
        SetupHookPayload();
        await ExecuteCommandAsync("agent", "hook", "opencode", "session-created");
        SetupHookPayload();
        await ExecuteCommandAsync("agent", "hook", "opencode", "chat-message");
        SetupHookPayload();

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "opencode", "chat-message");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task ChatMessage_Should_WriteNeutralResponse_When_ThePayloadIsMarkedNitroPushed()
    {
        // arrange: a marked, Nitro-pushed turn never receives the
        // announcement or digest this event exists to add.
        await InitWorkspaceAsync();
        await InsertOpencodeIdentityAsync("maya");
        SetupHookPayload();
        await ExecuteCommandAsync("agent", "hook", "opencode", "session-created");
        SetupStandardInput(
            $$"""
            {"sessionId":"{{SessionId}}","cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}},"serverUrl":"{{ServerUrl}}","nitroPushed":true}
            """);

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "opencode", "chat-message");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task SessionIdle_Should_WriteNeutralResponse_When_NoMailIsUnread()
    {
        // arrange: a presence row bound to the actor, with an empty inbox.
        await InitWorkspaceAsync();
        await InsertOpencodeIdentityAsync("maya");
        SetupHookPayload();
        await ExecuteCommandAsync("agent", "hook", "opencode", "session-created");
        SetupHookPayload();

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "opencode", "session-idle");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task SessionIdle_Should_ReserveTheUnreadMail_And_WriteNeutralResponse()
    {
        // arrange: a presence row bound to the actor, and one unread message
        // sent after the session started. The response body never carries
        // the reserved digest - opencode's idle push is delivered out of
        // band, not through this command's stdout.
        await InitWorkspaceAsync();
        await InsertOpencodeIdentityAsync("maya");
        await SeedAgentAsync("ada");
        SetupHookPayload();
        await ExecuteCommandAsync("agent", "hook", "opencode", "session-created");
        await ExecuteCommandAsync(
            "agent", "mail", "send", "--body", "All good.", "--to", "maya", "--subject", "Status", "--actor", "ada");
        SetupHookPayload();

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "opencode", "session-idle");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM session_deliveries"));
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task SessionDeleted_Should_RemoveThePresenceRow()
    {
        // arrange: a presence row this session started.
        await InitWorkspaceAsync();
        await InsertOpencodeIdentityAsync("maya");
        SetupHookPayload();
        await ExecuteCommandAsync("agent", "hook", "opencode", "session-created");
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM agent_sessions"));
        SetupHookPayload();

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "opencode", "session-deleted");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_sessions"));
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Theory]
    [InlineData("chat-message")]
    [InlineData("session-idle")]
    [InlineData("session-deleted")]
    public async Task Event_Should_WriteNeutralResponse_When_ThePayloadNamesNoSession(string eventName)
    {
        // arrange: a payload with no session id, so nothing identifies which
        // session the event speaks for.
        await InitWorkspaceAsync();
        await InsertOpencodeIdentityAsync("maya");
        SetupStandardInput(
            $$"""{"cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}},"serverUrl":"{{ServerUrl}}"}""");

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "opencode", eventName);

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task HookHelp_ShouldBeInvisible()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "--help");

        // assert
        result.AssertHelpOutput(string.Empty);
    }

    [Fact]
    public async Task OpencodeHelp_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "opencode", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Adapt opencode plugin shim turn-boundary events.

            Usage:
              nitro agent hook opencode [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              session-created  Adapt opencode's session.created event: register this session's presence row.
              chat-message     Adapt opencode's chat.message event: append the actor announcement and mail digest.
              session-idle     Adapt opencode's session.idle event: reserve unread mail for idle delivery.
              session-deleted  Adapt opencode's session.deleted event: remove this session's presence row.
            """);
    }

    [Theory]
    [InlineData(
        "session-created",
        "Adapt opencode's session.created event: register this session's presence row.")]
    [InlineData(
        "chat-message",
        "Adapt opencode's chat.message event: append the actor announcement and mail digest.")]
    [InlineData(
        "session-idle",
        "Adapt opencode's session.idle event: reserve unread mail for idle delivery.")]
    [InlineData(
        "session-deleted",
        "Adapt opencode's session.deleted event: remove this session's presence row.")]
    public async Task EventHelp_ReturnsSuccess(string eventName, string description)
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "opencode", eventName, "--help");

        // assert
        result.AssertHelpOutput(
            $"""
            Description:
              {description}

            Usage:
              nitro agent hook opencode {eventName} [options]

            Options:
              -?, -h, --help  Show help and usage information
            """);
    }

    private Task InsertOpencodeIdentityAsync(string actor)
        => InsertSessionIdentityAsync(actor, SessionId, AgentSessionHarness.Opencode);

    /// <summary>
    /// Feeds one shim event payload to stdin. Every command run consumes
    /// the reader, so a test invoking two events calls this again before
    /// the second.
    /// </summary>
    private void SetupHookPayload()
        => SetupStandardInput(
            $$"""
            {"sessionId":"{{SessionId}}","cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}},"serverUrl":"{{ServerUrl}}"}
            """);
}
