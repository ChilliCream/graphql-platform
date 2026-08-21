using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

public sealed class MemoryStoreTests : MemoryTestBase
{
    private readonly MemoryStore _store;

    public MemoryStoreTests() : base("nitro-memory-store-tests")
    {
        _store = new MemoryStore(FileSystem, TimeProvider, GlobalMemoryDirectory);
    }

    private async Task<MemoryRecord> SaveAsync(string scope, string text = "Some text.")
        => await _store.SaveAsync(
            new MemoryRecordCreation
            {
                Text = text,
                Type = "fact",
                Actor = "test-agent",
                Scope = scope
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task EnsureProjectWorkspaceAsync_Should_CreateCuratedJournalAndLocalDirectories()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        await _store.EnsureProjectWorkspaceAsync(WorkspaceDirectory, cancellationToken);

        // assert
        Assert.True(Directory.Exists(CuratedDirectory));
        Assert.True(Directory.Exists(JournalDirectory));
        Assert.True(Directory.Exists(LocalDirectory));
    }

    [Fact]
    public async Task EnsureProjectWorkspaceAsync_Should_BeIdempotent_When_CalledTwice()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _store.EnsureProjectWorkspaceAsync(WorkspaceDirectory, cancellationToken);
        var markerPath = Path.Combine(CuratedDirectory, "existing.md");
        File.WriteAllText(markerPath, "kept");

        // act
        await _store.EnsureProjectWorkspaceAsync(WorkspaceDirectory, cancellationToken);

        // assert
        Assert.Equal("kept", await File.ReadAllTextAsync(markerPath, cancellationToken));
    }

    [Fact]
    public void FindProjectWorkspaceDirectory_Should_ReturnNull_When_NoWorkspaceExists()
    {
        // arrange
        Directory.CreateDirectory(WorkingDirectory);

        // act
        var found = _store.FindProjectWorkspaceDirectory();

        // assert
        Assert.Null(found);
    }

    [Fact]
    public async Task FindProjectWorkspaceDirectory_Should_ReturnDirectory_When_ProvisionedByEnsure()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _store.EnsureProjectWorkspaceAsync(WorkspaceDirectory, cancellationToken);

        // act
        var found = _store.FindProjectWorkspaceDirectory();

        // assert
        Assert.Equal(WorkspaceDirectory, found);
    }

    [Fact]
    public async Task SaveAsync_Should_WriteToGlobalStore_When_ScopeIsGlobal_AndCreateItsDirectoriesLazily()
    {
        // act
        var record = await SaveAsync(MemoryScopes.Global);

        // assert
        Assert.Equal(MemoryScopes.Global, record.Scope);
        Assert.True(Directory.Exists(AgentWorkspace.GetMemoryCuratedDirectory(GlobalMemoryDirectory)));
        Assert.True(Directory.Exists(AgentWorkspace.GetMemoryJournalDirectory(GlobalMemoryDirectory)));
        Assert.True(Directory.Exists(AgentWorkspace.GetMemoryLocalDirectory(GlobalMemoryDirectory)));
        Assert.StartsWith(AgentWorkspace.GetMemoryCuratedDirectory(GlobalMemoryDirectory), record.Path);
    }

    [Fact]
    public async Task SaveAsync_Should_Throw_When_ScopeIsProject_AndNoWorkspaceExists()
    {
        // act
        var exception = await Record.ExceptionAsync(() => SaveAsync(MemoryScopes.Project));

        // assert
        var exitException = Assert.IsType<ExitException>(exception);
        Assert.Equal("No agent workspace found. Run `nitro agent init` first.", exitException.Message);
    }

    [Fact]
    public async Task SaveAsync_Should_NotFallBackToGlobal_When_ProjectScopeHasNoWorkspace()
    {
        // act
        await Record.ExceptionAsync(() => SaveAsync(MemoryScopes.Project));

        // assert: no lazily created global store as a silent fallback.
        Assert.False(Directory.Exists(GlobalMemoryDirectory));
    }

    [Fact]
    public async Task GetRecentCuratedAsync_Should_ReturnProjectBandFirst_ThenGlobalBand()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _store.EnsureProjectWorkspaceAsync(WorkspaceDirectory, cancellationToken);

        var globalFirst = await SaveAsync(MemoryScopes.Global, "Global first.");
        TimeProvider.Advance(TimeSpan.FromMinutes(1));
        var projectFirst = await SaveAsync(MemoryScopes.Project, "Project first.");
        TimeProvider.Advance(TimeSpan.FromMinutes(1));
        var globalSecond = await SaveAsync(MemoryScopes.Global, "Global second.");
        TimeProvider.Advance(TimeSpan.FromMinutes(1));
        var projectSecond = await SaveAsync(MemoryScopes.Project, "Project second.");

        // act
        var records = await _store.GetRecentCuratedAsync(MemoryScopes.All, limit: null, cancellationToken);

        // assert: project band (each entry newest first), then global band.
        Assert.Equal(
            [projectSecond.Id, projectFirst.Id, globalSecond.Id, globalFirst.Id],
            records.Select(record => record.Id).ToArray());
    }

    [Fact]
    public async Task GetRecentCuratedAsync_Should_ReturnOnlyGlobal_When_NoProjectWorkspaceExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var globalRecord = await SaveAsync(MemoryScopes.Global);

        // act
        var records = await _store.GetRecentCuratedAsync(MemoryScopes.All, limit: null, cancellationToken);

        // assert
        Assert.Equal([globalRecord.Id], records.Select(record => record.Id).ToArray());
    }

    [Fact]
    public async Task FindAsync_Should_ReturnRecord_When_ExistsInOnlyOneScope()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var record = await SaveAsync(MemoryScopes.Global);

        // act
        var found = await _store.FindAsync(record.Id, MemoryScopes.All, cancellationToken);

        // assert
        Assert.NotNull(found);
        Assert.Equal(MemoryScopes.Global, found.Scope);
    }

    [Fact]
    public async Task FindAsync_Should_Throw_MemoryScopeConflictException_When_SameIdExistsInBothScopes()
    {
        // arrange: a same-id collision between project and global is invalid
        // data (ids are collision-resistant) and only reachable by writing
        // outside the CLI, which this test simulates directly.
        var cancellationToken = TestContext.Current.CancellationToken;
        await _store.EnsureProjectWorkspaceAsync(WorkspaceDirectory, cancellationToken);
        var projectRecord = await SaveAsync(MemoryScopes.Project);

        var globalCuratedDirectory = AgentWorkspace.GetMemoryCuratedDirectory(GlobalMemoryDirectory);
        Directory.CreateDirectory(globalCuratedDirectory);
        File.Copy(
            projectRecord.Path,
            Path.Combine(globalCuratedDirectory, projectRecord.Id + ".md"));

        // act
        var exception = await Record.ExceptionAsync(
            () => _store.FindAsync(projectRecord.Id, MemoryScopes.All, cancellationToken));

        // assert
        var conflictException = Assert.IsType<MemoryScopeConflictException>(exception);
        var conflict = Assert.Single(conflictException.Conflicts);
        Assert.Equal(projectRecord.Id, conflict.Id);
        Assert.Equal([MemoryScopes.Project, MemoryScopes.Global], conflict.Scopes);
    }

    [Fact]
    public async Task FindAsync_Should_ReturnRecord_When_ScopeExplicitlyDisambiguatesAConflict()
    {
        // arrange: the same collision as above, but explicit scopes still
        // work for investigation.
        var cancellationToken = TestContext.Current.CancellationToken;
        await _store.EnsureProjectWorkspaceAsync(WorkspaceDirectory, cancellationToken);
        var projectRecord = await SaveAsync(MemoryScopes.Project);

        var globalCuratedDirectory = AgentWorkspace.GetMemoryCuratedDirectory(GlobalMemoryDirectory);
        Directory.CreateDirectory(globalCuratedDirectory);
        File.Copy(
            projectRecord.Path,
            Path.Combine(globalCuratedDirectory, projectRecord.Id + ".md"));

        // act
        var project = await _store.FindAsync(projectRecord.Id, MemoryScopes.Project, cancellationToken);
        var global = await _store.FindAsync(projectRecord.Id, MemoryScopes.Global, cancellationToken);

        // assert
        Assert.NotNull(project);
        Assert.Equal(MemoryScopes.Project, project.Scope);
        Assert.NotNull(global);
        Assert.Equal(MemoryScopes.Global, global.Scope);
    }

    [Fact]
    public async Task GetRecentCuratedAsync_Should_Throw_MemoryScopeConflictException_When_SameIdExistsInBothScopes()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _store.EnsureProjectWorkspaceAsync(WorkspaceDirectory, cancellationToken);
        var projectRecord = await SaveAsync(MemoryScopes.Project);

        var globalCuratedDirectory = AgentWorkspace.GetMemoryCuratedDirectory(GlobalMemoryDirectory);
        Directory.CreateDirectory(globalCuratedDirectory);
        File.Copy(
            projectRecord.Path,
            Path.Combine(globalCuratedDirectory, projectRecord.Id + ".md"));

        // act
        var exception = await Record.ExceptionAsync(
            () => _store.GetRecentCuratedAsync(MemoryScopes.All, limit: null, cancellationToken));

        // assert
        Assert.IsType<MemoryScopeConflictException>(exception);
    }

    [Fact]
    public async Task UpdateAsync_Should_Throw_When_ScopeIsProject_AndNoWorkspaceExists()
    {
        // act
        var exception = await Record.ExceptionAsync(() => _store.UpdateAsync(
            "01hqzxk8xdtd3fk3f0z7c5g8vm",
            MemoryScopes.Project,
            new MemoryRecordUpdate { Type = "decision", TypeGiven = true },
            TestContext.Current.CancellationToken));

        // assert
        var exitException = Assert.IsType<ExitException>(exception);
        Assert.Equal("No agent workspace found. Run `nitro agent init` first.", exitException.Message);
    }

    [Fact]
    public async Task ForgetAsync_Should_Throw_When_ScopeIsProject_AndNoWorkspaceExists()
    {
        // act
        var exception = await Record.ExceptionAsync(() => _store.ForgetAsync(
            "01hqzxk8xdtd3fk3f0z7c5g8vm", MemoryScopes.Project, TestContext.Current.CancellationToken));

        // assert
        var exitException = Assert.IsType<ExitException>(exception);
        Assert.Equal("No agent workspace found. Run `nitro agent init` first.", exitException.Message);
    }

    [Fact]
    public async Task UpdateAsync_Should_UpdateGlobalRecord_When_ScopeIsGlobal()
    {
        // arrange
        var record = await SaveAsync(MemoryScopes.Global);

        // act
        var updated = await _store.UpdateAsync(
            record.Id,
            MemoryScopes.Global,
            new MemoryRecordUpdate { Type = "decision", TypeGiven = true },
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(MemoryScopes.Global, updated.Scope);
        Assert.Equal("decision", updated.Type);
    }

    [Fact]
    public async Task ForgetAsync_Should_DeleteGlobalRecord_When_ScopeIsGlobal()
    {
        // arrange
        var record = await SaveAsync(MemoryScopes.Global);

        // act
        await _store.ForgetAsync(record.Id, MemoryScopes.Global, TestContext.Current.CancellationToken);

        // assert
        Assert.False(File.Exists(record.Path));
    }
}
