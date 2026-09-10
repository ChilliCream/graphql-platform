using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="CodexHarnessVersionResolver"/> against an injected
/// rollout-version reader (no real filesystem access).
/// </summary>
public sealed class CodexHarnessVersionResolverTests
{
    [Fact]
    public void Resolve_Should_ReturnTheRolloutVersion_When_Available()
    {
        // arrange
        var resolver = new CodexHarnessVersionResolver(rolloutVersionReader: _ => "0.101.0");

        // act
        var version = resolver.Resolve("session-1");

        // assert
        Assert.Equal("0.101.0", version);
    }

    [Fact]
    public void Resolve_Should_PassTheSessionId_ToTheRolloutReader()
    {
        // arrange
        var resolver = new CodexHarnessVersionResolver(rolloutVersionReader: id => id);

        // act
        var version = resolver.Resolve("session-1");

        // assert
        Assert.Equal("session-1", version);
    }

    [Fact]
    public void Resolve_Should_ReturnEmpty_When_NoRolloutResolves()
    {
        // arrange
        var resolver = new CodexHarnessVersionResolver(rolloutVersionReader: _ => null);

        // act
        var version = resolver.Resolve("session-1");

        // assert
        Assert.Equal("", version);
    }
}
