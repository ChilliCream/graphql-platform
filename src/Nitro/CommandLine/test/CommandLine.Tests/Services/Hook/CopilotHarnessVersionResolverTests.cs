using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="CopilotHarnessVersionResolver"/> against injected
/// session-state-version and exe-output readers (no real filesystem, process
/// exec, or <c>/proc</c> access). Both the <c>data.copilotVersion</c> field
/// path and the <c>--version</c> output grammar are live-verified against a
/// real <c>~/.copilot/session-state/&lt;id&gt;/events.jsonl</c> file.
/// </summary>
public sealed class CopilotHarnessVersionResolverTests
{
    [Fact]
    public void Resolve_Should_PreferSessionStateVersion_When_Available()
    {
        // arrange
        var resolver = new CopilotHarnessVersionResolver(
            sessionStateVersionReader: _ => "1.0.80",
            exeVersionOutputReader: _ => "GitHub Copilot CLI 1.0.35.");

        // act
        var version = resolver.Resolve("session-1", ancestorPid: 42);

        // assert: the session-state record is the exact version for THIS
        // session; an in-place self-update can leave the live exe reporting
        // a different one by the time a later event fires.
        Assert.Equal("1.0.80", version);
    }

    [Fact]
    public void Resolve_Should_FallBackToExeVersionOutput_When_SessionStateIsUnavailable()
    {
        // arrange: the reader already yields only the first output line
        // (ReadExeVersionOutput's real implementation stops at the first
        // line; a second advisory line is never part of what it returns).
        var resolver = new CopilotHarnessVersionResolver(
            sessionStateVersionReader: _ => null,
            exeVersionOutputReader: _ => "GitHub Copilot CLI 1.0.80.");

        // act
        var version = resolver.Resolve("session-1", ancestorPid: 42);

        // assert
        Assert.Equal("1.0.80", version);
    }

    [Fact]
    public void Resolve_Should_ReturnEmpty_When_NeitherSourceResolves()
    {
        // arrange
        var resolver = new CopilotHarnessVersionResolver(
            sessionStateVersionReader: _ => null,
            exeVersionOutputReader: _ => null);

        // act
        var version = resolver.Resolve("session-1", ancestorPid: 42);

        // assert
        Assert.Equal("", version);
    }

    [Theory]
    [InlineData("1.0.80")]
    [InlineData("not the expected grammar at all")]
    public void Resolve_Should_ReturnEmpty_When_ExeOutputDoesNotMatchTheGrammar(string output)
    {
        // arrange: missing the "GitHub Copilot CLI " prefix or the trailing
        // period both fail the grammar (the trailing period is part of it).
        var resolver = new CopilotHarnessVersionResolver(
            sessionStateVersionReader: _ => null,
            exeVersionOutputReader: _ => output);

        // act
        var version = resolver.Resolve("session-1", ancestorPid: 42);

        // assert
        Assert.Equal("", version);
    }
}
