using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Console;

public sealed class AgentModeDetectorTests
{
    [Theory]
    [InlineData("CLAUDECODE")]
    [InlineData("CLAUDE_CODE")]
    [InlineData("CODEX")]
    [InlineData("CURSOR_AGENT")]
    [InlineData("GITHUB_COPILOT")]
    [InlineData("CLINE")]
    [InlineData("WINDSURF_AGENT")]
    [InlineData("AIDER")]
    public void IsEnabled_Should_ReturnTrue_When_HarnessEnvironmentVariableIsSet(string environmentVariable)
    {
        // act
        var isEnabled = AgentModeDetector.IsEnabled(
            false,
            name => name == environmentVariable ? string.Empty : null);

        // assert
        Assert.True(isEnabled);
    }

    [Fact]
    public void IsEnabled_Should_ReturnTrue_When_StandardOutputIsRedirected()
    {
        // act
        var isEnabled = AgentModeDetector.IsEnabled(true, _ => null);

        // assert
        Assert.True(isEnabled);
    }

    [Fact]
    public void IsEnabled_Should_ReturnFalse_When_NoHarnessEnvironmentVariableIsSet()
    {
        // act
        var isEnabled = AgentModeDetector.IsEnabled(false, _ => null);

        // assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsInteractive_Should_ReturnFalse_When_AgentModeIsEnabled()
    {
        // arrange
        var outConsole = new TestConsole();
        outConsole.Profile.Capabilities.Interactive = true;
        var console = new NitroConsole(
            outConsole,
            new TestConsole(),
            new ActivitySinkFactory(),
            isAgentMode: true);

        // assert
        Assert.True(console.IsAgentMode);
        Assert.False(console.IsInteractive);
    }
}
