using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Tests <see cref="SessionDeliveryLedger"/> reservation uniqueness, channel
/// independence, delivery lookup, and session ownership against a real workspace database.
/// </summary>
public sealed class SessionDeliveryLedgerTests : IDisposable
{
    private const string Harness = "claude-code";
    private const string SessionId = "session-1";
    private static readonly AgentSessionGeneration s_generation = new(Harness, SessionId, "host-1");

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

        // act
        // The workspace directory has no initialized database.
        var reserved = await _ledger.ReserveAsync(
            s_generation, [], "digest", DateTimeOffset.UtcNow, cancellationToken);

        // assert
        Assert.Empty(reserved);
    }

    [Fact]
    public async Task FindDeliveredAsync_Should_ReturnEmpty_When_MessageIdsIsEmpty()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var generation = new AgentSessionGeneration(Harness, SessionId, "host-1");

        // act
        var delivered = await _ledger.FindDeliveredAsync(generation, [], cancellationToken);

        // assert
        Assert.Empty(delivered);
    }

    [Fact]
    public async Task FindDeliveredAsync_Should_ReturnDeliveredMessageIdsInInputOrder_When_DeliveredAcrossChannels()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var generation = new AgentSessionGeneration(Harness, SessionId, "host-1");
        await InitializeWorkspaceAndSessionAsync(cancellationToken, SessionId);
        await _ledger.ReserveAsync(
            Harness, SessionId, ["m-1"], AgentSessionChannel.Digest, DateTimeOffset.UtcNow, cancellationToken);
        await _ledger.ReserveAsync(
            Harness, SessionId, ["m-2"], AgentSessionChannel.Gate, DateTimeOffset.UtcNow, cancellationToken);
        await _ledger.ReserveAsync(
            Harness, SessionId, ["m-3"], AgentSessionChannel.Ping, DateTimeOffset.UtcNow, cancellationToken);

        // act
        var delivered = await _ledger.FindDeliveredAsync(
            generation, ["m-3", "m-missing", "m-1", "m-2"], cancellationToken);

        // assert
        Assert.Equal(["m-3", "m-1", "m-2"], delivered);
    }

    [Fact]
    public async Task FindDeliveredAsync_Should_IgnoreRowsFromDifferentSession()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var generation = new AgentSessionGeneration(Harness, SessionId, "host-1");
        await InitializeWorkspaceAndSessionAsync(cancellationToken, SessionId);
        await InitializeWorkspaceAndSessionAsync(cancellationToken, "session-2");
        await _ledger.ReserveAsync(
            Harness, "session-2", ["m-1"], AgentSessionChannel.Digest, DateTimeOffset.UtcNow, cancellationToken);

        // act
        var delivered = await _ledger.FindDeliveredAsync(generation, ["m-1"], cancellationToken);

        // assert
        Assert.Empty(delivered);
    }

    [Fact]
    public async Task ReserveAsync_Should_ReserveEveryNewMessageId()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAndSessionAsync(cancellationToken, SessionId);

        // act
        var reserved = await _ledger.ReserveAsync(
            s_generation, ["m-1", "m-2", "m-3"], "digest", DateTimeOffset.UtcNow, cancellationToken);

        // assert
        Assert.Equal(["m-1", "m-2", "m-3"], reserved);
    }

    [Fact]
    public async Task ReserveAsync_Should_ExcludeAlreadyReservedMessageId_When_CalledAgain()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAndSessionAsync(cancellationToken, SessionId);
        await _ledger.ReserveAsync(
            s_generation, ["m-1", "m-2"], "digest", DateTimeOffset.UtcNow, cancellationToken);

        // act
        var reserved = await _ledger.ReserveAsync(
            s_generation, ["m-1", "m-2", "m-3"], "digest", DateTimeOffset.UtcNow, cancellationToken);

        // assert
        Assert.Equal(["m-3"], reserved);
    }

    [Fact]
    public async Task ReserveAsync_Should_ReserveIndependently_When_ChannelDiffers()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAndSessionAsync(cancellationToken, SessionId);
        await _ledger.ReserveAsync(
            s_generation, ["m-1"], "digest", DateTimeOffset.UtcNow, cancellationToken);

        // act
        var reservedGate = await _ledger.ReserveAsync(
            s_generation, ["m-1"], "gate", DateTimeOffset.UtcNow, cancellationToken);
        var reservedPing = await _ledger.ReserveAsync(
            s_generation, ["m-1"], "ping", DateTimeOffset.UtcNow, cancellationToken);

        // assert
        Assert.Equal(["m-1"], reservedGate);
        Assert.Equal(["m-1"], reservedPing);
    }

    [Fact]
    public async Task ReserveAsync_Should_SplitReservationExactlyOnce_When_TwoSimultaneousHandlersRaceTheSameMessage()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAndSessionAsync(cancellationToken, SessionId);

        // act
        var results = await Task.WhenAll(
            _ledger.ReserveAsync(s_generation, ["m-1"], "gate", DateTimeOffset.UtcNow, cancellationToken),
            _ledger.ReserveAsync(s_generation, ["m-1"], "gate", DateTimeOffset.UtcNow, cancellationToken));

        // assert
        var totalReserved = results.Sum(r => r.Count);
        Assert.Equal(1, totalReserved);
    }

    [Fact]
    public async Task ReserveAsync_Should_ExcludeAStaleGeneration_When_TheSessionHostWasReplaced()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var replacement = new AgentSessionGeneration(Harness, SessionId, "host-2");
        await InitializeWorkspaceAndSessionAsync(cancellationToken, SessionId);
        await ReplaceSessionHostAsync(replacement.Host, cancellationToken);

        // act
        var staleReserved = await _ledger.ReserveAsync(
            s_generation, ["m-1"], "gate", DateTimeOffset.UtcNow, cancellationToken);
        var replacementReserved = await _ledger.ReserveAsync(
            replacement, ["m-1"], "gate", DateTimeOffset.UtcNow, cancellationToken);

        // assert
        Assert.Empty(staleReserved);
        Assert.Equal(["m-1"], replacementReserved);
    }

    private async Task InitializeWorkspaceAndSessionAsync(CancellationToken cancellationToken, string sessionId)
    {
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', @sessionId, NULL, 'none', 'host-1',
                '/work', '/work/.nitro/agents', 'none', '', '2026-01-10T12:00:00Z', '2026-01-10T12:00:00Z'
            );
            """;

        command.Parameters.AddWithValue("@sessionId", sessionId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ReplaceSessionHostAsync(string host, CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE agent_sessions SET host = @host WHERE harness = @harness AND session_id = @sessionId";
        command.Parameters.AddWithValue("@host", host);
        command.Parameters.AddWithValue("@harness", Harness);
        command.Parameters.AddWithValue("@sessionId", SessionId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
