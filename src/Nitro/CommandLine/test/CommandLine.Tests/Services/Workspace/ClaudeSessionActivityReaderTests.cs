using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.HookRuntime;

[Collection(HomeDirectoryCollection.Name)]
public sealed class ClaudeSessionActivityReaderTests
{
    [Fact]
    public void GetStatus_Should_ReturnNull_When_StatusIsNotAString()
    {
        // arrange
        var reader = new ClaudeSessionActivityReader(sessionFileReader: _ => "{\"status\":1}");

        // act
        var status = reader.GetStatus("session-1");

        // assert
        Assert.Null(status);
    }

    [Fact]
    public void GetStatus_Should_ReturnNull_When_DiscoveredSessionFileRootIsAnArray()
    {
        // arrange
        using var home = new TemporaryHomeDirectory();
        var directory = Path.Combine(home.Root.FullName, ".claude", "sessions");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "session.json"), "[]");
        var reader = new ClaudeSessionActivityReader();

        // act
        var status = reader.GetStatus("session-1");

        // assert
        Assert.Null(status);
    }

    [Fact]
    public void GetStatus_Should_ReturnNull_When_SessionFileRootIsAnArray()
    {
        // arrange
        var reader = new ClaudeSessionActivityReader(sessionFileReader: _ => "[]");

        // act
        var status = reader.GetStatus("session-1");

        // assert
        Assert.Null(status);
    }
}
