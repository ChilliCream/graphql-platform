using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.HookRuntime;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="CodexHarnessVersionResolver"/> against injected readers
/// and temporary rollout files.
/// </summary>
[Collection(HomeDirectoryCollection.Name)]
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

    [Fact]
    public void Resolve_Should_ReturnEmpty_When_RolloutRecordTypeIsNotAString()
    {
        // arrange
        using var home = new TemporaryHomeDirectory();
        WriteRolloutFile(home, "{\"type\":1}");
        var resolver = new CodexHarnessVersionResolver();

        // act
        var version = resolver.Resolve("session-1");

        // assert
        Assert.Equal("", version);
    }

    [Fact]
    public void Resolve_Should_ReturnEmpty_When_RolloutFileRootIsAnArray()
    {
        // arrange
        using var home = new TemporaryHomeDirectory();
        WriteRolloutFile(home, "[]");
        var resolver = new CodexHarnessVersionResolver();

        // act
        var version = resolver.Resolve("session-1");

        // assert
        Assert.Equal("", version);
    }

    [Theory]
    [InlineData("{\"type\":\"session_meta\",\"payload\":[]}")]
    [InlineData("{\"type\":\"session_meta\",\"payload\":{\"cli_version\":1}}")]
    public void Resolve_Should_ReturnEmpty_When_RolloutPayloadIsNotWellFormed(string json)
    {
        // arrange
        using var home = new TemporaryHomeDirectory();
        WriteRolloutFile(home, json);
        var resolver = new CodexHarnessVersionResolver();

        // act
        var version = resolver.Resolve("session-1");

        // assert
        Assert.Equal("", version);
    }

    private static void WriteRolloutFile(TemporaryHomeDirectory home, string json)
    {
        var directory = Path.Combine(home.Root.FullName, ".codex", "sessions");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "rollout-test-session-1.jsonl"), json);
    }
}
