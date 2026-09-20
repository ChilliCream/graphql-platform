using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.HookRuntime;

[Collection(HomeDirectoryCollection.Name)]
public sealed class ClaudeSessionFileReaderTests
{
    [Fact]
    public void Find_Should_ReturnNull_When_SessionFileRootIsANumber()
    {
        // arrange
        using var home = new TemporaryHomeDirectory();
        WriteSessionFile(home, "1");
        var reader = new ClaudeSessionFileReader();

        // act
        var session = reader.Find("session-1");

        // assert
        Assert.Null(session);
    }

    [Fact]
    public void Find_Should_ReturnNull_When_SessionFileRootIsAnArray()
    {
        // arrange
        using var home = new TemporaryHomeDirectory();
        WriteSessionFile(home, "[]");
        var reader = new ClaudeSessionFileReader();

        // act
        var session = reader.Find("session-1");

        // assert
        Assert.Null(session);
    }

    private static void WriteSessionFile(TemporaryHomeDirectory home, string json)
    {
        var directory = Path.Combine(home.Root.FullName, ".claude", "sessions");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "session.json"), json);
    }
}
