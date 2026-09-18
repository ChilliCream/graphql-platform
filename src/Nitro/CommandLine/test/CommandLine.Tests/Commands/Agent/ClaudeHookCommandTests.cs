using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers the command's wiring and the response it writes to stdout. The
/// event state machine, digest, gate, and ledger behavior are exercised
/// directly against <see cref="Services.Hook.ClaudeHookHandler"/> in
/// <c>ClaudeHookHandlerTests</c>, and the fail-open envelope against
/// <see cref="Services.Hook.ClaudeHookExecutor"/> in
/// <c>ClaudeHookExecutorTests</c>.
/// </summary>
public sealed class ClaudeHookCommandTests(NitroCommandFixture fixture) : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task SessionStart_Should_WriteTheActorContext_ToStdout()
    {
        // arrange: an identity already bound to this session id, so the
        // announced actor is the seeded name rather than an allocated one.
        await InitWorkspaceAsync();
        await InsertSessionIdentityAsync("maya", "session-1");
        SetupStandardInput(
            $$"""{"session_id":"session-1","cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}}}""");

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "claude", "session-start");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().MatchInlineSnapshot(
            """{"hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext":"Your Nitro actor name is \u0022maya\u0022. Pass this name to the \u0060--actor\u0060 option to act under this actor explicitly."}}""");
    }

    [Fact]
    public async Task SessionStart_Should_WriteNeutralResponse_When_ThePayloadNamesNoSession()
    {
        // arrange: a payload with no session id, so nothing identifies which
        // session the event speaks for.
        await InitWorkspaceAsync();
        SetupStandardInput(
            $$"""{"cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}}}""");

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "claude", "session-start");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task UserPromptSubmit_Should_WriteNeutralResponse_When_NoMailIsUnread()
    {
        // arrange: an identity already bound to this session id, and an
        // empty inbox. The actor name is announced by session-start alone,
        // so with no mail to report this event has nothing to say.
        await InitWorkspaceAsync();
        await InsertSessionIdentityAsync("maya", "session-1");
        SetupStandardInput(
            $$"""{"session_id":"session-1","cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}}}""");

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "claude", "user-prompt-submit");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task UserPromptSubmit_Should_AppendTheMailDigest_When_TheActorHasUnreadMail()
    {
        // arrange: one unread message addressed to the actor this session
        // is bound to, sent by a second allocated actor.
        await InitWorkspaceAsync();
        await InsertSessionIdentityAsync("maya", "session-1");
        var message = await SeedMailAsync();
        SetupStandardInput(
            $$"""{"session_id":"session-1","cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}}}""");

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "claude", "user-prompt-submit");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().Replace(message.Id, "<id>").MatchInlineSnapshot(
            """
            {"hookSpecificOutput":{"hookEventName":"UserPromptSubmit","additionalContext":"You have 1 unread nitro message; 1 shown below as \u0060nitro agent mail read --thread --output json\u0060 prints them. Reply with \u0060nitro agent mail reply --message \u003Cid\u003E --actor maya --body \u0022...\u0022\u0060 or ack with \u0060nitro agent mail ack --message \u003Cid\u003E --actor maya\u0060; anything not shown is in \u0060nitro agent mail inbox --unread --actor maya\u0060.\n{\n  \u0022items\u0022: [\n    {\n      \u0022id\u0022: \u0022<id>\u0022,\n      \u0022threadId\u0022: \u0022<id>\u0022,\n      \u0022inReplyTo\u0022: null,\n      \u0022from\u0022: \u0022ada\u0022,\n      \u0022to\u0022: [\n        \u0022maya\u0022\n      ],\n      \u0022cc\u0022: [],\n      \u0022subject\u0022: \u0022Status\u0022,\n      \u0022body\u0022: \u0022All good.\u0022,\n      \u0022createdAt\u0022: \u00222026-01-01T00:00:00\u002B00:00\u0022,\n      \u0022read\u0022: false,\n      \u0022archived\u0022: false,\n      \u0022takeovers\u0022: []\n    }\n  ]\n}"}}
            """);
    }

    [Fact]
    public async Task Stop_Should_WriteNeutralResponse_When_NoUnreadMailIsUndelivered()
    {
        // arrange: a presence row bound to the actor, with an empty inbox,
        // so the gate has nothing to block the turn for.
        await InitWorkspaceAsync();
        await InsertSessionIdentityAsync("maya", "session-1");
        SetupStandardInput(
            $$"""{"session_id":"session-1","cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}}}""");
        await ExecuteCommandAsync("agent", "hook", "claude", "session-start");
        Assert.Equal("maya", await QueryScalarAsync("SELECT agent_name FROM agent_sessions"));
        SetupStandardInput(
            $$"""{"session_id":"session-1","cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}}}""");

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "claude", "stop");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task Stop_Should_BlockTheTurn_When_UnreadMailIsUndelivered()
    {
        // arrange: a presence row bound to the actor, and one unread
        // message never yet delivered on the gate channel.
        await InitWorkspaceAsync();
        await InsertSessionIdentityAsync("maya", "session-1");
        var message = await SeedMailAsync();
        SetupStandardInput(
            $$"""{"session_id":"session-1","cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}}}""");
        await ExecuteCommandAsync("agent", "hook", "claude", "session-start");
        SetupStandardInput(
            $$"""{"session_id":"session-1","cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}}}""");

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "claude", "stop");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().Replace(message.Id, "<id>").MatchInlineSnapshot(
            """
            {"decision":"block","reason":"Unread nitro mail is waiting; handle it before ending this turn, or ignore this once if it is not actionable right now.\nYou have 1 unread nitro message; 1 shown below as \u0060nitro agent mail read --thread --output json\u0060 prints them. Reply with \u0060nitro agent mail reply --message \u003Cid\u003E --actor maya --body \u0022...\u0022\u0060 or ack with \u0060nitro agent mail ack --message \u003Cid\u003E --actor maya\u0060; anything not shown is in \u0060nitro agent mail inbox --unread --actor maya\u0060.\n{\n  \u0022items\u0022: [\n    {\n      \u0022id\u0022: \u0022<id>\u0022,\n      \u0022threadId\u0022: \u0022<id>\u0022,\n      \u0022inReplyTo\u0022: null,\n      \u0022from\u0022: \u0022ada\u0022,\n      \u0022to\u0022: [\n        \u0022maya\u0022\n      ],\n      \u0022cc\u0022: [],\n      \u0022subject\u0022: \u0022Status\u0022,\n      \u0022body\u0022: \u0022All good.\u0022,\n      \u0022createdAt\u0022: \u00222026-01-01T00:00:00\u002B00:00\u0022,\n      \u0022read\u0022: false,\n      \u0022archived\u0022: false,\n      \u0022takeovers\u0022: []\n    }\n  ]\n}"}
            """);
    }

    [Fact]
    public async Task SessionEnd_Should_RemoveThePresenceRow()
    {
        // arrange: a presence row this session started.
        await InitWorkspaceAsync();
        await InsertSessionIdentityAsync("maya", "session-1");
        SetupStandardInput(
            $$"""{"session_id":"session-1","cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}}}""");
        await ExecuteCommandAsync("agent", "hook", "claude", "session-start");
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM agent_sessions"));
        SetupStandardInput(
            $$"""{"session_id":"session-1","cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}}}""");

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "claude", "session-end");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_sessions"));
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Theory]
    [InlineData("user-prompt-submit")]
    [InlineData("stop")]
    [InlineData("session-end")]
    public async Task Event_Should_WriteNeutralResponse_When_ThePayloadNamesNoSession(string eventName)
    {
        // arrange: a payload with no session id, so nothing identifies which
        // session the event speaks for.
        await InitWorkspaceAsync();
        await InsertSessionIdentityAsync("maya", "session-1");
        SetupStandardInput(
            $$"""{"cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}}}""");

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "claude", eventName);

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
    public async Task ClaudeHelp_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "claude", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Adapt Claude Code turn-boundary hook events.

            Usage:
              nitro agent hook claude [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              session-start       Adapt Claude Code's SessionStart hook: upsert this session's presence row.
              user-prompt-submit  Adapt Claude Code's UserPromptSubmit hook: reset the block budget and inject the unread-mail digest.
              stop                Adapt Claude Code's Stop hook: block the turn while unread mail is undelivered.
              session-end         Adapt Claude Code's SessionEnd hook: delete this session's presence row.
            """);
    }

    [Theory]
    [InlineData(
        "session-start",
        "Adapt Claude Code's SessionStart hook: upsert this session's presence row.")]
    [InlineData(
        "user-prompt-submit",
        "Adapt Claude Code's UserPromptSubmit hook: reset the block budget and inject the unread-mail digest.")]
    [InlineData(
        "stop",
        "Adapt Claude Code's Stop hook: block the turn while unread mail is undelivered.")]
    [InlineData(
        "session-end",
        "Adapt Claude Code's SessionEnd hook: delete this session's presence row.")]
    public async Task EventHelp_ReturnsSuccess(string eventName, string description)
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "claude", eventName, "--help");

        // assert
        result.AssertHelpOutput(
            $"""
            Description:
              {description}

            Usage:
              nitro agent hook claude {eventName} [options]

            Options:
              -?, -h, --help  Show help and usage information
            """);
    }

    private async Task<MailMessage> SeedMailAsync()
    {
        await SeedAgentAsync("ada");

        var store = new MailStore(
            new TestFileSystem(WorkingDirectory),
            FakeTime,
            new AgentDatabase(),
            new AgentRegistry(new TestFileSystem(WorkingDirectory), FakeTime, new AgentDatabase()));

        return await store.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = "ada",
                Subject = "Status",
                Body = "All good.",
                To = ["maya"]
            },
            TestContext.Current.CancellationToken);
    }
}
