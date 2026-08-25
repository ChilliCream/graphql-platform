namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers command wiring (help text) only, same reasoning as
/// <c>CodexHookCommandTests</c>: the adapter and executor behavior is
/// exercised directly against
/// <see cref="ChilliCream.Nitro.CommandLine.Services.Hook.CopilotHookHandler"/>
/// / <see cref="ChilliCream.Nitro.CommandLine.Services.Hook.CopilotHookExecutor"/>
/// in <c>CopilotHookHandlerTests</c>/<c>CopilotHookExecutorTests</c>; this
/// command's own action reads real stdin, which the test host cannot supply
/// deterministically.
/// </summary>
public sealed class CopilotHookCommandTests(NitroCommandFixture fixture) : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task HookHelp_ListsCopilotAlongsideClaudeAndCodex()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("claude", result.StdOut);
        Assert.Contains("codex", result.StdOut);
        Assert.Contains("copilot", result.StdOut);
    }

    [Fact]
    public async Task CopilotHelp_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "copilot", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Adapt GitHub Copilot CLI turn-boundary hook events.

            Usage:
              nitro agent hook copilot [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              session-start       Adapt Copilot CLI's sessionStart hook: upsert this session's presence row and return the initial unread-mail digest.
              user-prompt-submit  Adapt Copilot CLI's userPromptSubmitted hook: a documented no-op, this event's response body is dropped by Copilot.
              session-end         Adapt Copilot CLI's sessionEnd hook: delete this session's presence row.
            """);
    }

    [Theory]
    [InlineData(
        "session-start",
        "Adapt Copilot CLI's sessionStart hook: upsert this session's presence row and return the initial unread-mail digest.")]
    [InlineData(
        "user-prompt-submit",
        "Adapt Copilot CLI's userPromptSubmitted hook: a documented no-op, this event's response body is dropped by Copilot.")]
    [InlineData("session-end", "Adapt Copilot CLI's sessionEnd hook: delete this session's presence row.")]
    public async Task EventHelp_ReturnsSuccess(string eventName, string description)
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "copilot", eventName, "--help");

        // assert
        result.AssertHelpOutput(
            $"""
            Description:
              {description}

            Usage:
              nitro agent hook copilot {eventName} [options]

            Options:
              -?, -h, --help  Show help and usage information
            """);
    }
}
