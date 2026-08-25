using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="ClaudeHarnessVersionResolver"/> against injected
/// session-file and start-tick readers (no real <c>/proc</c> or
/// <c>~/.claude/sessions</c> access), using the session registry shape
/// (<c>{"pid":...,"procStart":"39270330","version":"2.1.241",...}</c>).
/// </summary>
public sealed class ClaudeHarnessVersionResolverTests
{
    [Fact]
    public void Resolve_Should_ReturnVersion_When_StartTicksMatch()
    {
        // arrange
        var resolver = new ClaudeHarnessVersionResolver(
            sessionFileReader: _ => """{"pid":107466,"procStart":"39270330","version":"2.1.241"}""",
            startTicksReader: _ => "39270330");

        // act
        var version = resolver.Resolve(107466);

        // assert
        Assert.Equal("2.1.241", version);
    }

    [Fact]
    public void Resolve_Should_ReturnEmpty_When_StartTicksDisagree()
    {
        // arrange: the pid was reused since the session file was written -
        // its recorded version can no longer be trusted for the CURRENT
        // process at that pid.
        var resolver = new ClaudeHarnessVersionResolver(
            sessionFileReader: _ => """{"pid":107466,"procStart":"39270330","version":"2.1.241"}""",
            startTicksReader: _ => "99999999");

        // act
        var version = resolver.Resolve(107466);

        // assert
        Assert.Equal("", version);
    }

    [Fact]
    public void Resolve_Should_ReturnEmpty_When_ActualStartTicksAreUnavailable()
    {
        // arrange
        var resolver = new ClaudeHarnessVersionResolver(
            sessionFileReader: _ => """{"pid":107466,"procStart":"39270330","version":"2.1.241"}""",
            startTicksReader: _ => null);

        // act
        var version = resolver.Resolve(107466);

        // assert
        Assert.Equal("", version);
    }

    [Fact]
    public void Resolve_Should_ReturnEmpty_When_SessionFileIsMissing()
    {
        // arrange
        var resolver = new ClaudeHarnessVersionResolver(
            sessionFileReader: _ => null,
            startTicksReader: _ => "39270330");

        // act
        var version = resolver.Resolve(107466);

        // assert
        Assert.Equal("", version);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"pid":107466}""")]
    [InlineData("""{"version":"2.1.241"}""")]
    public void Resolve_Should_ReturnEmpty_When_SessionFileIsMalformedOrIncomplete(string json)
    {
        // arrange
        var resolver = new ClaudeHarnessVersionResolver(
            sessionFileReader: _ => json,
            startTicksReader: _ => "39270330");

        // act
        var version = resolver.Resolve(107466);

        // assert
        Assert.Equal("", version);
    }
}
