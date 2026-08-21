using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

public sealed class AgentWorkspaceMemoryTests : MemoryTestBase
{
    public AgentWorkspaceMemoryTests() : base("nitro-memory-agent-workspace-tests")
    {
    }

    [Fact]
    public void GetMemoryDirectory_Should_NestUnderTheAgentWorkspace()
    {
        // act & assert
        Assert.Equal(Path.Combine(WorkspaceDirectory, "memory"), MemoryDirectory);
        Assert.Equal(Path.Combine(MemoryDirectory, "curated"), CuratedDirectory);
        Assert.Equal(Path.Combine(MemoryDirectory, "journal"), JournalDirectory);
        Assert.Equal(Path.Combine(MemoryDirectory, ".local"), LocalDirectory);
    }

    [Fact]
    public void GetGlobalMemoryDirectory_Should_NestUnderApplicationDataNitro()
    {
        // arrange
        var applicationDataDirectory = Path.Combine(WorkingDirectory, "app-data");

        // act
        var globalMemoryDirectory = AgentWorkspace.GetGlobalMemoryDirectory(applicationDataDirectory);

        // assert
        Assert.Equal(Path.Combine(applicationDataDirectory, "nitro", "memory"), globalMemoryDirectory);
    }

    [Fact]
    public void FindMemory_Should_ReturnDirectory_When_DatabaseExists()
    {
        // arrange
        Directory.CreateDirectory(WorkspaceDirectory);
        File.WriteAllText(AgentWorkspace.GetDatabasePath(WorkspaceDirectory), "");

        // act
        var found = AgentWorkspace.FindMemory(FileSystem, WorkingDirectory);

        // assert
        Assert.Equal(WorkspaceDirectory, found);
    }

    [Fact]
    public void FindMemory_Should_ReturnDirectory_When_OnlyCuratedMarkdownExistsWithNoDatabase()
    {
        // arrange: a fresh clone of a repository that committed curated
        // memory but has not run `agent init` (or `sync`) yet.
        Directory.CreateDirectory(CuratedDirectory);
        File.WriteAllText(Path.Combine(CuratedDirectory, "01hqzxk8xdtd3fk3f0z7c5g8vm.md"), "---\n---\n");

        // act
        var found = AgentWorkspace.FindMemory(FileSystem, WorkingDirectory);

        // assert
        Assert.Equal(WorkspaceDirectory, found);
    }

    [Fact]
    public void FindMemory_Should_ReturnDirectory_When_OnlyJournalMarkdownExistsWithNoDatabase()
    {
        // arrange
        var dayDirectory = Path.Combine(JournalDirectory, "2026-01-10");
        Directory.CreateDirectory(dayDirectory);
        File.WriteAllText(Path.Combine(dayDirectory, "01hqzxk8xdtd3fk3f0z7c5g8vm.md"), "---\n---\n");

        // act
        var found = AgentWorkspace.FindMemory(FileSystem, WorkingDirectory);

        // assert
        Assert.Equal(WorkspaceDirectory, found);
    }

    [Fact]
    public void FindMemory_Should_ReturnNull_When_NeitherDatabaseNorMemoryMarkdownExists()
    {
        // arrange
        Directory.CreateDirectory(WorkingDirectory);

        // act
        var found = AgentWorkspace.FindMemory(FileSystem, WorkingDirectory);

        // assert
        Assert.Null(found);
    }

    [Fact]
    public void GitIgnoreContent_Should_IgnoreTheMemoryLocalDirectory()
        => Assert.Contains("memory/.local/", AgentWorkspace.GitIgnoreContent);
}
