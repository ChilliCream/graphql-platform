using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="BoardSessionLifecycle"/>: binding the resolved human
/// mail actor as the operator on a <see cref="AgentSessionHarness.NitroBoard"/>
/// session with a <see cref="AgentSessionEndpointKind.DbWatch"/> endpoint,
/// deterministic session ids per process generation (unique across distinct
/// generations, idempotent for a duplicate call against the same one),
/// heartbeat, cleanup, startup failure, and never touching an
/// independently configured durable identity's own role or client.
/// </summary>
public sealed class BoardSessionLifecycleTests : IDisposable
{
    private const string Host = "host-board";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentDatabase _database;
    private readonly AgentRegistry _agentRegistry;
    private readonly AgentSessionRegistry _sessions;

    public BoardSessionLifecycleTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-board-session-lifecycle-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        _database = new AgentDatabase();
        _agentRegistry = new AgentRegistry(_fileSystem, _timeProvider, _database);
        _sessions = new AgentSessionRegistry(
            _fileSystem,
            _timeProvider,
            _database,
            _agentRegistry,
            new FixedInstanceIdProvider(Host),
            new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName),
            new ProcessInfoProvider(),
            new FixedAncestorSessionResolver(null));
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task StartAsync_Should_BindOperatorRoleDbWatchEndpointAndCliVersion()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var lifecycle = CreateLifecycle();

        // act
        var generation = await lifecycle.StartAsync("pascal", cancellationToken);

        // assert
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.NotNull(row);
        Assert.Equal(AgentSessionHarness.NitroBoard, row!.Harness);
        Assert.Equal("pascal", row.AgentName);
        Assert.Equal(AgentSessionBindingKind.Env, row.BindingKind);
        Assert.Equal(AgentSessionEndpointKind.DbWatch, row.EndpointKind);
        Assert.Equal("operator", row.Role);
        Assert.Equal(NitroCliVersion.Current, row.HarnessVersion);
    }

    [Fact]
    public async Task StartAsync_Should_ResolveToTheSameRow_When_CalledTwiceForTheSameProcessGeneration()
    {
        // arrange: a duplicate StartAsync call for the exact same process
        // generation (host, pid, proc_start) must be idempotent, not
        // register a second row.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var lifecycle = CreateLifecycle();

        // act
        var first = await lifecycle.StartAsync("pascal", cancellationToken);
        var second = await lifecycle.StartAsync("pascal", cancellationToken);

        // assert
        Assert.Equal(first, second);
        var host = await ResolveHostAsync(cancellationToken);
        var rows = await _sessions.FindByProcessAsync(
            AgentSessionHarness.NitroBoard, host, Environment.ProcessId, first.ProcStart, cancellationToken);
        Assert.Single(rows);
    }

    [Fact]
    public async Task StartAsync_Should_ProduceDistinctSessions_When_TheProcessGenerationDiffers()
    {
        // arrange: two distinct process generations - simulating two
        // separate board launches, even sharing the same OS pid across a
        // restart - must never collide onto the same row.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        var firstProcessInfo = new FakeProcessInfoProvider();
        firstProcessInfo.SetAlive(Environment.ProcessId, "111111");
        var firstLifecycle = CreateLifecycle(firstProcessInfo);

        var secondProcessInfo = new FakeProcessInfoProvider();
        secondProcessInfo.SetAlive(Environment.ProcessId, "222222");
        var secondLifecycle = CreateLifecycle(secondProcessInfo);

        // act
        var first = await firstLifecycle.StartAsync("pascal", cancellationToken);
        var second = await secondLifecycle.StartAsync("pascal", cancellationToken);

        // assert
        Assert.NotEqual(first.SessionId, second.SessionId);
        var host = await ResolveHostAsync(cancellationToken);
        var rows = await _sessions.FindByProcessAsync(
            AgentSessionHarness.NitroBoard, host, Environment.ProcessId, first.ProcStart, cancellationToken);
        Assert.Single(rows);
        var otherRows = await _sessions.FindByProcessAsync(
            AgentSessionHarness.NitroBoard, host, Environment.ProcessId, second.ProcStart, cancellationToken);
        Assert.Single(otherRows);
    }

    [Fact]
    public async Task TouchAsync_Should_AdvanceLastBeatAt()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var lifecycle = CreateLifecycle();
        var generation = await lifecycle.StartAsync("pascal", cancellationToken);
        var startedAt = (await _sessions.FindByGenerationAsync(generation, cancellationToken))!.LastBeatAt;
        _timeProvider.Advance(TimeSpan.FromMinutes(1));

        // act
        await lifecycle.TouchAsync(generation, cancellationToken);

        // assert
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.True(row!.LastBeatAt > startedAt);
    }

    [Fact]
    public async Task EndAsync_Should_RemoveTheLiveRow_And_LeaveTheDurableIdentityAndReturnTrue()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var lifecycle = CreateLifecycle();
        var generation = await lifecycle.StartAsync("pascal", cancellationToken);

        // act
        var removed = await lifecycle.EndAsync(generation, cancellationToken);

        // assert: closing the board removes only live presence; the durable
        // actor identity survives.
        Assert.True(removed);
        Assert.Null(await _sessions.FindByGenerationAsync(generation, cancellationToken));
        Assert.NotNull(await _agentRegistry.GetAsync("pascal", cancellationToken));
    }

    [Fact]
    public async Task EndAsync_Should_ReturnFalse_When_AlreadyEnded()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var lifecycle = CreateLifecycle();
        var generation = await lifecycle.StartAsync("pascal", cancellationToken);
        await lifecycle.EndAsync(generation, cancellationToken);

        // act
        var removedAgain = await lifecycle.EndAsync(generation, cancellationToken);

        // assert
        Assert.False(removedAgain);
    }

    [Fact]
    public async Task StartAsync_Should_AppearOnlineWithOperatorRole_And_DisappearAfterEndAsync_InTheParticipantList()
    {
        // arrange: the Agents-tab read model - a live board session shows as
        // online (the same presence computation any other harness gets) with
        // its operator role, and the row is gone once the board closes.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var lifecycle = CreateLifecycle();
        var generation = await lifecycle.StartAsync("pascal", cancellationToken);

        // act
        var participantsWhileOpen = await _sessions.ListParticipantsAsync(cancellationToken);

        // assert
        var boardParticipant = Assert.Single(
            participantsWhileOpen, p => p.Session.Harness == AgentSessionHarness.NitroBoard);
        Assert.Equal(AgentSessionState.Online, boardParticipant.State);
        Assert.Equal("operator", boardParticipant.Session.Role);
        Assert.Equal("pascal", boardParticipant.Session.AgentName);

        // act: close the board.
        await lifecycle.EndAsync(generation, cancellationToken);
        var participantsAfterClose = await _sessions.ListParticipantsAsync(cancellationToken);

        // assert
        Assert.Equal(0, participantsAfterClose.Count(p => p.Session.Harness == AgentSessionHarness.NitroBoard));
    }

    [Fact]
    public async Task StartAsync_Should_Throw_When_NoAgentWorkspaceFound()
    {
        // arrange: no InitializeWorkspaceAsync call, so AgentWorkspace.Find
        // resolves nothing from the current directory.
        var cancellationToken = TestContext.Current.CancellationToken;
        var lifecycle = CreateLifecycle();

        // act & assert
        await Assert.ThrowsAsync<ExitException>(() => lifecycle.StartAsync("pascal", cancellationToken));
    }

    [Fact]
    public async Task StartAsync_Should_Throw_When_ThisProcesssOwnStartTicksCannotBeRead()
    {
        // arrange: stands in for a startup failure reading this process's
        // own generation.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var lifecycle = CreateLifecycle(new FakeProcessInfoProvider());

        // act & assert
        await Assert.ThrowsAsync<ExitException>(() => lifecycle.StartAsync("pascal", cancellationToken));
    }

    [Fact]
    public async Task StartAsync_Should_NotOverwriteAnIndependentlyConfiguredDurableIdentity()
    {
        // arrange: "pascal" already carries its own durable role and client
        // from an earlier, unrelated registration (for example a task-audit
        // identity). The board session's own operator role is a separate,
        // session-scoped column - it must never relabel the durable identity.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _agentRegistry.RegisterAsync("pascal", role: "reviewer", client: "custom-tool", cancellationToken);
        var lifecycle = CreateLifecycle();

        // act
        var generation = await lifecycle.StartAsync("pascal", cancellationToken);

        // assert
        var identity = await _agentRegistry.GetAsync("pascal", cancellationToken);
        Assert.Equal("reviewer", identity!.Role);
        Assert.Equal("custom-tool", identity.Client);

        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.Equal("operator", row!.Role);
    }

    private BoardSessionLifecycle CreateLifecycle(IProcessInfoProvider? processInfoProvider = null)
        => new(
            _fileSystem,
            _sessions,
            new FixedInstanceIdProvider(Host),
            new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName),
            processInfoProvider ?? new ProcessInfoProvider());

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }

    private async Task<string> ResolveHostAsync(CancellationToken cancellationToken)
        => await new FixedInstanceIdProvider(Host).GetIdAsync(_tempRoot.FullName, cancellationToken);
}
