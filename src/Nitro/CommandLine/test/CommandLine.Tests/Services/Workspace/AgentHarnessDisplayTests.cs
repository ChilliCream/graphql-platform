using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="AgentHarnessDisplay"/>: the canonical harness ids and the
/// login-only fallback.
/// </summary>
public sealed class AgentHarnessDisplayTests
{
    [Theory]
    [InlineData("claude-code", "Claude Code")]
    [InlineData("codex", "Codex")]
    [InlineData("copilot", "Copilot")]
    [InlineData("opencode", "OpenCode")]
    [InlineData(null, "-")]
    [InlineData("", "-")]
    public void Name_Should_ReturnDisplayName_When_GivenAHarnessId(string? harness, string expected)
    {
        // act
        var name = AgentHarnessDisplay.Name(harness);

        // assert
        Assert.Equal(expected, name);
    }
}
