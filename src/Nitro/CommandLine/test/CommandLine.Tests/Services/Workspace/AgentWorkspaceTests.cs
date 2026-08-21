using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class AgentWorkspaceTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot;

    public AgentWorkspaceTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-agent-workspace-tests");
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public void Find_Should_ReturnDirectory_When_DatabaseExistsAtStart()
    {
        // arrange
        var workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(workspaceDirectory);
        File.WriteAllText(AgentWorkspace.GetDatabasePath(workspaceDirectory), "");
        var fileSystem = new TestFileSystem(_tempRoot.FullName);

        // act
        var found = AgentWorkspace.Find(fileSystem, _tempRoot.FullName);

        // assert
        Assert.Equal(workspaceDirectory, found);
    }

    [Fact]
    public void Find_Should_WalkUpToParent_When_DatabaseExistsAboveStart()
    {
        // arrange
        var workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(workspaceDirectory);
        File.WriteAllText(AgentWorkspace.GetDatabasePath(workspaceDirectory), "");

        var nestedDirectory = Path.Combine(_tempRoot.FullName, "src", "nested");
        Directory.CreateDirectory(nestedDirectory);
        var fileSystem = new TestFileSystem(nestedDirectory);

        // act
        var found = AgentWorkspace.Find(fileSystem, nestedDirectory);

        // assert
        Assert.Equal(workspaceDirectory, found);
    }

    [Fact]
    public void Find_Should_ReturnNull_When_NoWorkspaceExists()
    {
        // arrange
        var fileSystem = new TestFileSystem(_tempRoot.FullName);

        // act
        var found = AgentWorkspace.Find(fileSystem, _tempRoot.FullName);

        // assert
        Assert.Null(found);
    }

    [Fact]
    public void Find_Should_ReturnNull_When_OnlyJsonlExists()
    {
        // arrange
        var workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(workspaceDirectory);
        File.WriteAllText(AgentWorkspace.GetJsonlPath(workspaceDirectory), "");
        var fileSystem = new TestFileSystem(_tempRoot.FullName);

        // act
        var found = AgentWorkspace.Find(fileSystem, _tempRoot.FullName);

        // assert
        Assert.Null(found);
    }

    /// <summary>
    /// Covers the fresh-clone bootstrap: a workspace containing only the
    /// committed tasks.jsonl (and .gitignore), no database yet, as a freshly
    /// cloned repository would look before <c>sync --import-only</c> builds
    /// the database.
    /// </summary>
    [Fact]
    public void FindDatabaseOrJsonl_Should_ReturnDirectory_When_OnlyJsonlExists()
    {
        // arrange
        var workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(workspaceDirectory);
        File.WriteAllText(AgentWorkspace.GetJsonlPath(workspaceDirectory), "");
        File.WriteAllText(
            Path.Combine(workspaceDirectory, AgentWorkspace.GitIgnoreFileName),
            AgentWorkspace.GitIgnoreContent);
        var fileSystem = new TestFileSystem(_tempRoot.FullName);

        // act
        var found = AgentWorkspace.FindDatabaseOrJsonl(fileSystem, _tempRoot.FullName);

        // assert
        Assert.Equal(workspaceDirectory, found);
    }

    [Fact]
    public void FindDatabaseOrJsonl_Should_ReturnDirectory_When_OnlyDatabaseExists()
    {
        // arrange
        var workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(workspaceDirectory);
        File.WriteAllText(AgentWorkspace.GetDatabasePath(workspaceDirectory), "");
        var fileSystem = new TestFileSystem(_tempRoot.FullName);

        // act
        var found = AgentWorkspace.FindDatabaseOrJsonl(fileSystem, _tempRoot.FullName);

        // assert
        Assert.Equal(workspaceDirectory, found);
    }

    [Fact]
    public void FindDatabaseOrJsonl_Should_ReturnNull_When_NeitherExists()
    {
        // arrange
        var fileSystem = new TestFileSystem(_tempRoot.FullName);

        // act
        var found = AgentWorkspace.FindDatabaseOrJsonl(fileSystem, _tempRoot.FullName);

        // assert
        Assert.Null(found);
    }

    [Theory]
    [InlineData("acme", "acme")]
    [InlineData("My App!", "myapp")]
    [InlineData("---", "task")]
    [InlineData("", "task")]
    public void NormalizePrefix_Should_LowercaseAndStripInvalidCharacters(string value, string expected)
    {
        // act
        var normalized = AgentWorkspace.NormalizePrefix(value);

        // assert
        Assert.Equal(expected, normalized);
    }
}
