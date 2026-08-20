using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

public sealed class MailWorkspaceTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot;

    public MailWorkspaceTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-mail-workspace-tests");
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public void Find_Should_ReturnDirectory_When_DatabaseExistsAtStart()
    {
        // arrange
        var workspaceDirectory = MailWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(workspaceDirectory);
        File.WriteAllText(MailWorkspace.GetDatabasePath(workspaceDirectory), "");
        var fileSystem = new TestFileSystem(_tempRoot.FullName);

        // act
        var found = MailWorkspace.Find(fileSystem, _tempRoot.FullName);

        // assert
        Assert.Equal(workspaceDirectory, found);
    }

    [Fact]
    public void Find_Should_WalkUpToParent_When_DatabaseExistsAboveStart()
    {
        // arrange
        var workspaceDirectory = MailWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(workspaceDirectory);
        File.WriteAllText(MailWorkspace.GetDatabasePath(workspaceDirectory), "");

        var nestedDirectory = Path.Combine(_tempRoot.FullName, "src", "nested");
        Directory.CreateDirectory(nestedDirectory);
        var fileSystem = new TestFileSystem(nestedDirectory);

        // act
        var found = MailWorkspace.Find(fileSystem, nestedDirectory);

        // assert
        Assert.Equal(workspaceDirectory, found);
    }

    [Fact]
    public void Find_Should_ReturnNull_When_NoWorkspaceExists()
    {
        // arrange
        var fileSystem = new TestFileSystem(_tempRoot.FullName);

        // act
        var found = MailWorkspace.Find(fileSystem, _tempRoot.FullName);

        // assert
        Assert.Null(found);
    }
}
