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
        SetupAncestorSessionResolvers(
            claude: new ClaudeAncestorSession(Environment.ProcessId, "session-1", WorkingDirectory, ""));
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
    public async Task SessionStart_Should_WriteNeutralResponse_When_NoSessionResolves()
    {
        // arrange: no ancestor harness session, so nothing identifies this
        // process as a coding session.
        await InitWorkspaceAsync();
        SetupStandardInput(
            $$"""{"session_id":"session-1","cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}}}""");

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "claude", "session-start");

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
}
