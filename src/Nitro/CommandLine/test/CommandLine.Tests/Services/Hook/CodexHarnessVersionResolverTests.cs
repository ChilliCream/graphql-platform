using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="CodexHarnessVersionResolver"/> against injected
/// rollout-version and exe-path readers (no real filesystem or <c>/proc</c>
/// access). The exe-path regex is live-verified against
/// <c>~/.codex/packages/standalone/releases/&lt;version&gt;-&lt;triple&gt;/bin/codex</c>
/// paths per perles-net-xy9.2 comment 124's correction.
/// </summary>
public sealed class CodexHarnessVersionResolverTests
{
    [Fact]
    public void Resolve_Should_PreferRolloutVersion_When_Available()
    {
        // arrange
        var resolver = new CodexHarnessVersionResolver(
            rolloutVersionReader: _ => "0.101.0",
            exePathReader: _ => "/home/user/.codex/packages/standalone/releases/0.150.0-x86_64-unknown-linux-musl/bin/codex");

        // act
        var version = resolver.Resolve("session-1", ancestorPid: 42);

        // assert: the rollout is the exact version for THIS session; the
        // live exe could since have been upgraded to a different one.
        Assert.Equal("0.101.0", version);
    }

    [Theory]
    [InlineData(
        "/home/user/.codex/packages/standalone/releases/0.149.1-x86_64-unknown-linux-musl/bin/codex", "0.149.1")]
    [InlineData(
        "/Users/user/.codex/packages/standalone/releases/0.149.1-aarch64-apple-darwin/bin/codex", "0.149.1")]
    [InlineData(
        "/home/user/.codex/packages/standalone/releases/0.150.0-alpha.1-aarch64-apple-darwin/bin/codex",
        "0.150.0-alpha.1")]
    public void Resolve_Should_FallBackToExePathVersion_When_RolloutIsUnavailable(string exePath, string expected)
    {
        // arrange
        var resolver = new CodexHarnessVersionResolver(
            rolloutVersionReader: _ => null,
            exePathReader: _ => exePath);

        // act
        var version = resolver.Resolve("session-1", ancestorPid: 42);

        // assert
        Assert.Equal(expected, version);
    }

    [Fact]
    public void Resolve_Should_ReturnEmpty_When_NeitherSourceResolves()
    {
        // arrange
        var resolver = new CodexHarnessVersionResolver(
            rolloutVersionReader: _ => null,
            exePathReader: _ => null);

        // act
        var version = resolver.Resolve("session-1", ancestorPid: 42);

        // assert
        Assert.Equal("", version);
    }

    [Fact]
    public void Resolve_Should_ReturnEmpty_When_ExePathDoesNotMatchAReleaseLayout()
    {
        // arrange: a PATH-selected or dev-built binary, not a standalone
        // release install.
        var resolver = new CodexHarnessVersionResolver(
            rolloutVersionReader: _ => null,
            exePathReader: _ => "/usr/local/bin/codex");

        // act
        var version = resolver.Resolve("session-1", ancestorPid: 42);

        // assert
        Assert.Equal("", version);
    }
}
