using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.HookRuntime;

public sealed class ClaudeSessionFileReaderTests
{
    [Fact]
    public void Find_Should_ReturnNull_When_SessionFileRootIsANumber()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        WriteSessionFile(directory, "1");
        var reader = new ClaudeSessionFileReader();

        // act
        var session = reader.Find(directory.Root.FullName, "session-1");

        // assert
        Assert.Null(session);
    }

    [Fact]
    public void Find_Should_ReturnNull_When_SessionFileRootIsAnArray()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        WriteSessionFile(directory, "[]");
        var reader = new ClaudeSessionFileReader();

        // act
        var session = reader.Find(directory.Root.FullName, "session-1");

        // assert
        Assert.Null(session);
    }

    private static void WriteSessionFile(TemporaryDirectory directory, string json)
    {
        File.WriteAllText(Path.Combine(directory.Root.FullName, "session.json"), json);
    }
}
