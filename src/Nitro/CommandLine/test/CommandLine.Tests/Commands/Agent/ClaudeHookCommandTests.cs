namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers command wiring (help text) only. The event state machine, digest,
/// gate, and ledger behavior are exercised directly against
/// <see cref="ChilliCream.Nitro.CommandLine.Services.Hook.ClaudeHookHandler"/>
/// in <c>ClaudeHookHandlerTests</c>, and the fail-open envelope, stdin
/// parsing, and captured payload fixtures against
/// <see cref="ChilliCream.Nitro.CommandLine.Services.Hook.ClaudeHookExecutor"/>
/// in <c>ClaudeHookExecutorTests</c>: this command's own action reads real
/// stdin, which the test host cannot supply deterministically (mirrors
/// <c>ClaimSessionCommandTests</c>'s reasoning for staying wiring-only here).
/// </summary>
public sealed class ClaudeHookCommandTests(NitroCommandFixture fixture) : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task HookHelp_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Translate harness turn-boundary hook payloads into digest and gate behavior.

            Usage:
              nitro agent hook [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              claude  Adapt Claude Code turn-boundary hook events.
            """);
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
              --dry-run       Resolve the process identity from this process itself instead of walking its ancestors for a live Claude Code parent, so a captured payload fixture can drive this adapter without a real Claude Code session above it
              -?, -h, --help  Show help and usage information
            """);
    }
}
