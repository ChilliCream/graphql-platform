using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.HookRuntime;

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
        using var directory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(directory.Root.FullName, "session.json"), "[]");
        var reader = new ClaudeSessionActivityReader(
            sessionFileReader: sessionId =>
                ClaudeSessionActivityReader.ReadSessionFile(directory.Root.FullName, sessionId));

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
