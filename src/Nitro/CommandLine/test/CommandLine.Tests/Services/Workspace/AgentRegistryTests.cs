using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="AgentRegistry"/> against a real SQLite workspace:
/// register, touch, get, and list, including role and implicit semantics.
/// </summary>
public sealed class AgentRegistryTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot;
    private readonly string _workingDirectory;
    private readonly string _workspaceDirectory;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentRegistry _registry;

    public AgentRegistryTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-agent-registry-tests");
        _workingDirectory = Path.Combine(_tempRoot.FullName, "acme");
        Directory.CreateDirectory(_workingDirectory);
        _workspaceDirectory = AgentWorkspace.GetDirectory(_workingDirectory);

        _timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));

        _registry = new AgentRegistry(new TestFileSystem(_workingDirectory), _timeProvider, new AgentDatabase());
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    private async Task InitWorkspaceAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_workspaceDirectory);
        await new AgentDatabase().InitializeAsync(_workspaceDirectory, cancellationToken);
    }

    [Fact]
    public async Task RegisterAsync_Should_InsertNewAgent_When_NotRegistered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var agent = await _registry.RegisterAsync("Claude", role: "", cancellationToken);

        // assert
        Assert.Equal("claude", agent.Name);
        Assert.Equal("", agent.Role);
        Assert.False(agent.Implicit);
        Assert.Equal(_timeProvider.GetUtcNow(), agent.RegisteredAt);
        Assert.Equal(_timeProvider.GetUtcNow(), agent.LastSeenAt);
    }

    [Fact]
    public async Task RegisterAsync_Should_NormalizeRoleLowercase()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var agent = await _registry.RegisterAsync("claude", role: "Backend", cancellationToken);

        // assert
        Assert.Equal("backend", agent.Role);
    }

    [Fact]
    public async Task RegisterAsync_Should_BumpLastSeenAt_When_AlreadyRegistered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var first = await _registry.RegisterAsync("claude", role: "", cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var second = await _registry.RegisterAsync("claude", role: "", cancellationToken);

        // assert
        Assert.Equal(first.RegisteredAt, second.RegisteredAt);
        Assert.Equal(_timeProvider.GetUtcNow(), second.LastSeenAt);
        Assert.NotEqual(second.RegisteredAt, second.LastSeenAt);
    }

    [Fact]
    public async Task RegisterAsync_Should_OverwriteRole_When_CalledAgainWithoutRole()
    {
        // arrange: register always sets role to the resolved value, the
        // empty string when omitted, the same way last_seen_at is always
        // bumped; TouchAsync is the path that preserves an existing role.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _registry.RegisterAsync("claude", role: "backend", cancellationToken);

        // act
        var agent = await _registry.RegisterAsync("claude", role: "", cancellationToken);

        // assert
        Assert.Equal("", agent.Role);
    }

    [Fact]
    public async Task RegisterAsync_Should_ClearImplicit_When_RowWasImplicit()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _registry.TouchAsync("claude", cancellationToken);

        // act
        var agent = await _registry.RegisterAsync("claude", role: "backend", cancellationToken);

        // assert
        Assert.False(agent.Implicit);
    }

    [Fact]
    public async Task TouchAsync_Should_InsertNewAgent_WithEmptyRole_When_NotRegistered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var agent = await _registry.TouchAsync("claude", cancellationToken);

        // assert
        Assert.Equal("claude", agent.Name);
        Assert.Equal("", agent.Role);
        Assert.False(agent.Implicit);
    }

    [Fact]
    public async Task TouchAsync_Should_PreserveRole_When_AlreadyRegisteredWithRole()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _registry.RegisterAsync("claude", role: "backend", cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var agent = await _registry.TouchAsync("claude", cancellationToken);

        // assert
        Assert.Equal("backend", agent.Role);
        Assert.Equal(_timeProvider.GetUtcNow(), agent.LastSeenAt);
    }

    [Fact]
    public async Task EnsureImplicitAsync_Should_InsertImplicitAgent_When_NotRegistered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var agent = await _registry.EnsureImplicitAsync("dave", cancellationToken);

        // assert
        Assert.Equal("dave", agent.Name);
        Assert.Equal("", agent.Role);
        Assert.True(agent.Implicit);
        Assert.Equal(_timeProvider.GetUtcNow(), agent.RegisteredAt);
        Assert.Equal(_timeProvider.GetUtcNow(), agent.LastSeenAt);
    }

    [Fact]
    public async Task EnsureImplicitAsync_Should_ReturnExistingRowUnchanged_When_AlreadyRegistered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var registered = await _registry.RegisterAsync("dave", role: "backend", cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var agent = await _registry.EnsureImplicitAsync("dave", cancellationToken);

        // assert
        Assert.Equal("backend", agent.Role);
        Assert.False(agent.Implicit);
        Assert.Equal(registered.LastSeenAt, agent.LastSeenAt);
    }

    [Fact]
    public async Task EnsureImplicitAsync_Should_ReturnExistingImplicitRowUnchanged_When_AlreadyImplicit()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var first = await _registry.EnsureImplicitAsync("dave", cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var second = await _registry.EnsureImplicitAsync("dave", cancellationToken);

        // assert
        Assert.True(second.Implicit);
        Assert.Equal(first.LastSeenAt, second.LastSeenAt);
    }

    [Fact]
    public async Task GetAsync_Should_ReturnNull_When_NotRegistered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var agent = await _registry.GetAsync("nobody", cancellationToken);

        // assert
        Assert.Null(agent);
    }

    [Fact]
    public async Task ListAsync_Should_ReturnAllOrderedByName_When_NoFilterGiven()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _registry.RegisterAsync("bob", role: "", cancellationToken);
        await _registry.RegisterAsync("alice", role: "", cancellationToken);

        // act
        var agents = await _registry.ListAsync(role: null, staleBefore: null, cancellationToken);

        // assert
        Assert.Equal(["alice", "bob"], agents.Select(a => a.Name));
    }

    [Fact]
    public async Task ListAsync_Should_FilterByExactRole()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _registry.RegisterAsync("alice", role: "backend", cancellationToken);
        await _registry.RegisterAsync("bob", role: "frontend", cancellationToken);

        // act
        var agents = await _registry.ListAsync(role: "backend", staleBefore: null, cancellationToken);

        // assert
        var agent = Assert.Single(agents);
        Assert.Equal("alice", agent.Name);
    }

    [Fact]
    public async Task ListAsync_Should_FilterByStaleBefore()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _registry.RegisterAsync("stale-agent", role: "", cancellationToken);
        _timeProvider.Advance(TimeSpan.FromDays(31));
        await _registry.RegisterAsync("fresh-agent", role: "", cancellationToken);

        // act
        var agents = await _registry.ListAsync(
            role: null, staleBefore: _timeProvider.GetUtcNow() - TimeSpan.FromDays(30), cancellationToken);

        // assert
        var agent = Assert.Single(agents);
        Assert.Equal("stale-agent", agent.Name);
    }

    [Fact]
    public async Task ListAsync_Should_FilterByRoleAndStaleBefore_Together()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _registry.RegisterAsync("stale-backend", role: "backend", cancellationToken);
        await _registry.RegisterAsync("stale-frontend", role: "frontend", cancellationToken);
        _timeProvider.Advance(TimeSpan.FromDays(31));
        await _registry.RegisterAsync("fresh-backend", role: "backend", cancellationToken);

        // act
        var agents = await _registry.ListAsync(
            role: "backend", staleBefore: _timeProvider.GetUtcNow() - TimeSpan.FromDays(30), cancellationToken);

        // assert
        var agent = Assert.Single(agents);
        Assert.Equal("stale-backend", agent.Name);
    }

    [Fact]
    public async Task ListAsync_Should_Throw_When_NoWorkspaceExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _registry.ListAsync(role: null, staleBefore: null, cancellationToken));
    }
}
