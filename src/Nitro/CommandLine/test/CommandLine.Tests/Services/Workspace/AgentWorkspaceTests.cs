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
    public void Find_Should_ReturnNull_When_OnlyLegacyJsonlExists()
    {
        // arrange
        var workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(workspaceDirectory);
        File.WriteAllText(AgentWorkspace.GetLegacyJsonlPath(workspaceDirectory), "");
        var fileSystem = new TestFileSystem(_tempRoot.FullName);

        // act
        var found = AgentWorkspace.Find(fileSystem, _tempRoot.FullName);

        // assert
        Assert.Null(found);
    }

    [Fact]
    public void ResolveGitCommonDirectory_Should_ReturnGitDirectory_When_GitFolderExists()
    {
        // arrange
        var gitDirectory = Path.Combine(_tempRoot.FullName, ".git");
        Directory.CreateDirectory(gitDirectory);
        var fileSystem = new TestFileSystem(_tempRoot.FullName);

        // act
        var resolved = AgentWorkspace.ResolveGitCommonDirectory(fileSystem, _tempRoot.FullName);

        // assert
        Assert.Equal(gitDirectory, resolved);
    }

    [Fact]
    public void ResolveGitCommonDirectory_Should_FollowWorktreePointer_ToCommonDirectory()
    {
        // arrange: a linked worktree's .git file pointing at the main
        // checkout's gitdir, which redirects to the common directory.
        var mainGitDirectory = Path.Combine(_tempRoot.FullName, "main", ".git");
        var worktreeGitDirectory = Path.Combine(mainGitDirectory, "worktrees", "wt");
        Directory.CreateDirectory(worktreeGitDirectory);
        File.WriteAllText(Path.Combine(worktreeGitDirectory, "commondir"), "../..\n");

        var worktreeRoot = Path.Combine(_tempRoot.FullName, "wt");
        Directory.CreateDirectory(worktreeRoot);
        File.WriteAllText(
            Path.Combine(worktreeRoot, ".git"),
            "gitdir: ../main/.git/worktrees/wt\n");
        var fileSystem = new TestFileSystem(worktreeRoot);

        // act
        var resolved = AgentWorkspace.ResolveGitCommonDirectory(fileSystem, worktreeRoot);

        // assert
        Assert.Equal(Path.GetFullPath(mainGitDirectory), resolved);
    }

    [Fact]
    public void ResolveGitCommonDirectory_Should_ReturnNull_When_GitFileIsMalformed()
    {
        // arrange
        File.WriteAllText(Path.Combine(_tempRoot.FullName, ".git"), "not a pointer\n");
        var fileSystem = new TestFileSystem(_tempRoot.FullName);

        // act
        var resolved = AgentWorkspace.ResolveGitCommonDirectory(fileSystem, _tempRoot.FullName);

        // assert
        Assert.Null(resolved);
    }

    [Fact]
    public void FindLocation_Should_PreferNitroWorkspace_Over_GitWorkspace_AtSameLevel()
    {
        // arrange: both layouts initialized side by side.
        var fallbackDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(fallbackDirectory);
        File.WriteAllText(AgentWorkspace.GetDatabasePath(fallbackDirectory), "");

        var gitWorkspaceDirectory = Path.Combine(_tempRoot.FullName, ".git", "nitro");
        Directory.CreateDirectory(gitWorkspaceDirectory);
        File.WriteAllText(AgentWorkspace.GetDatabasePath(gitWorkspaceDirectory), "");
        var fileSystem = new TestFileSystem(_tempRoot.FullName);

        // act
        var location = AgentWorkspace.FindLocation(fileSystem, _tempRoot.FullName);

        // assert
        Assert.Equal(fallbackDirectory, location?.WorkspaceDirectory);
    }

    [Fact]
    public void FindLocation_Should_ReturnGitWorkspace_When_OnlyGitDatabaseExists()
    {
        // arrange
        var gitWorkspaceDirectory = Path.Combine(_tempRoot.FullName, ".git", "nitro");
        Directory.CreateDirectory(gitWorkspaceDirectory);
        File.WriteAllText(AgentWorkspace.GetDatabasePath(gitWorkspaceDirectory), "");

        var nestedDirectory = Path.Combine(_tempRoot.FullName, "src", "nested");
        Directory.CreateDirectory(nestedDirectory);
        var fileSystem = new TestFileSystem(nestedDirectory);

        // act
        var location = AgentWorkspace.FindLocation(fileSystem, nestedDirectory);

        // assert
        Assert.Equal(gitWorkspaceDirectory, location?.WorkspaceDirectory);
        Assert.Equal(_tempRoot.FullName, location?.ProjectDirectory);
    }

    [Theory]
    [InlineData("/repo/.nitro/agents", ".nitro/agents")]
    [InlineData("/repo/.git/nitro", ".git/nitro")]
    [InlineData("/repo/.git/modules/sub/nitro", "/repo/.git/modules/sub/nitro")]
    public void GetDisplayPath_Should_ShortenStandardLayouts(string workspaceDirectory, string expected)
    {
        // act & assert
        Assert.Equal(expected, AgentWorkspace.GetDisplayPath(workspaceDirectory));
    }

    [Theory]
    [InlineData("/repo/.nitro/agents", true)]
    [InlineData("/repo/.git/nitro", false)]
    [InlineData("/repo/other/agents", false)]
    public void IsFallbackLayout_Should_DetectNitroAgentsLayout(string workspaceDirectory, bool expected)
    {
        // act & assert
        Assert.Equal(expected, AgentWorkspace.IsFallbackLayout(workspaceDirectory));
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
