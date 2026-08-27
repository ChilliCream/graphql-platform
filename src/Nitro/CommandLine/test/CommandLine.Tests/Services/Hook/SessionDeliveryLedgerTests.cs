using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="SessionDeliveryLedger"/>'s reserve-then-emit
/// contract directly against a real workspace database: at-most-once
/// reservation per (session, message, channel), channel independence, and
/// the crash-between-reserve-and-emit and simultaneous-handler scenarios
/// named in the mail notification plan's required tests.
/// </summary>
public sealed class SessionDeliveryLedgerTests : IDisposable
{
    private const string Harness = "claude-code";
    private const string SessionId = "session-1";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly AgentDatabase _database;
    private readonly SessionDeliveryLedger _ledger;

    public SessionDeliveryLedgerTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-session-delivery-ledger-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _database = new AgentDatabase();
        _ledger = new SessionDeliveryLedger(_fileSystem, _database);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task ReserveAsync_Should_ReturnEmpty_When_MessageIdsIsEmpty()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // act: no workspace was even initialized - proves this short-circuits
        // before opening a connection.
        var reserved = await _ledger.ReserveAsync(
            Harness, SessionId, [], "digest", DateTimeOffset.UtcNow, cancellationToken);

        // assert
        Assert.Empty(reserved);
    }

    [Fact]
    public async Task ReserveAsync_Should_ReserveEveryNewMessageId()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAndSessionAsync(cancellationToken);

        // act
        var reserved = await _ledger.ReserveAsync(
            Harness, SessionId, ["m-1", "m-2", "m-3"], "digest", DateTimeOffset.UtcNow, cancellationToken);

        // assert
        Assert.Equal(["m-1", "m-2", "m-3"], reserved);
    }

    [Fact]
    public async Task ReserveAsync_Should_ExcludeAlreadyReservedMessageId_When_CalledAgain()
    {
        // arrange: simulates crash-between-reserve-and-emit - the first call
        // reserves and is treated as delivered even though nothing ever
        // "emits" past it, and a later call for the same message on the
        // same channel must not reserve it a second time.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAndSessionAsync(cancellationToken);
        await _ledger.ReserveAsync(
            Harness, SessionId, ["m-1", "m-2"], "digest", DateTimeOffset.UtcNow, cancellationToken);

        // act
        var reserved = await _ledger.ReserveAsync(
            Harness, SessionId, ["m-1", "m-2", "m-3"], "digest", DateTimeOffset.UtcNow, cancellationToken);

        // assert
        Assert.Equal(["m-3"], reserved);
    }

    [Fact]
    public async Task ReserveAsync_Should_ReserveIndependently_When_ChannelDiffers()
    {
        // arrange: a digest reservation must never suppress the same message
        // on the gate or ping channel.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAndSessionAsync(cancellationToken);
        await _ledger.ReserveAsync(
            Harness, SessionId, ["m-1"], "digest", DateTimeOffset.UtcNow, cancellationToken);

        // act
        var reservedGate = await _ledger.ReserveAsync(
            Harness, SessionId, ["m-1"], "gate", DateTimeOffset.UtcNow, cancellationToken);
        var reservedPing = await _ledger.ReserveAsync(
            Harness, SessionId, ["m-1"], "ping", DateTimeOffset.UtcNow, cancellationToken);

        // assert
        Assert.Equal(["m-1"], reservedGate);
        Assert.Equal(["m-1"], reservedPing);
    }

    [Fact]
    public async Task ReserveAsync_Should_SplitReservationExactlyOnce_When_TwoSimultaneousHandlersRaceTheSameMessage()
    {
        // arrange: two "handlers" (Stop and a retried notify, for example)
        // race to reserve the same message on the same channel.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAndSessionAsync(cancellationToken);

        // act
        var results = await Task.WhenAll(
            _ledger.ReserveAsync(Harness, SessionId, ["m-1"], "gate", DateTimeOffset.UtcNow, cancellationToken),
            _ledger.ReserveAsync(Harness, SessionId, ["m-1"], "gate", DateTimeOffset.UtcNow, cancellationToken));

        // assert: exactly one of the two calls won the reservation, never
        // both and never neither.
        var totalReserved = results.Sum(r => r.Count);
        Assert.Equal(1, totalReserved);
    }

    private async Task InitializeWorkspaceAndSessionAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', 'session-1', NULL, 'none', 'host-1',
                '/work', '/work/.nitro/agents', 'none', '', '2026-01-10T12:00:00Z', '2026-01-10T12:00:00Z'
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
