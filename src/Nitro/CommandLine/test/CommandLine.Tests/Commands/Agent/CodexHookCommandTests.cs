namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers command wiring (help text) only, same reasoning as
/// <c>ClaudeHookCommandTests</c>: the adapter and executor behavior is
/// exercised directly against
/// <see cref="ChilliCream.Nitro.CommandLine.Services.Hook.CodexHookHandler"/>
/// / <see cref="ChilliCream.Nitro.CommandLine.Services.Hook.CodexHookExecutor"/>
/// in <c>CodexHookHandlerTests</c>/<c>CodexHookExecutorTests</c>; this
/// command's own action reads real stdin (or, for <c>notify</c>, spawns real
/// child processes), which the test host cannot supply deterministically.
/// </summary>
public sealed class CodexHookCommandTests(NitroCommandFixture fixture) : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task HookHelp_StaysHidden()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdOut);
        Assert.Empty(result.StdErr);
    }

    [Fact]
    public async Task CodexHelp_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "codex", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Adapt Codex CLI turn-boundary hook and notify events.

            Usage:
              nitro agent hook codex [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              session-start       Adapt Codex CLI's SessionStart hook: upsert this session's presence row.
              user-prompt-submit  Adapt Codex CLI's UserPromptSubmit hook: inject the unread-mail digest.
              session-end         Adapt Codex CLI's SessionEnd hook: delete this session's presence row.
              notify <payload>    Adapt Codex CLI's notify program: queue the unread-mail digest into the thread's next turn, then exec any wrapped foreign notify program.
            """);
    }

    [Theory]
    [InlineData("session-start", "Adapt Codex CLI's SessionStart hook: upsert this session's presence row.")]
    [InlineData("user-prompt-submit", "Adapt Codex CLI's UserPromptSubmit hook: inject the unread-mail digest.")]
    [InlineData("session-end", "Adapt Codex CLI's SessionEnd hook: delete this session's presence row.")]
    public async Task EventHelp_ReturnsSuccess(string eventName, string description)
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "codex", eventName, "--help");

        // assert
        result.AssertHelpOutput(
            $"""
            Description:
              {description}

            Usage:
              nitro agent hook codex {eventName} [options]

            Options:
              -?, -h, --help  Show help and usage information
            """);
    }

    [Fact]
    public async Task NotifyHelp_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "codex", "notify", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("payload", result.StdOut);
    }
}
