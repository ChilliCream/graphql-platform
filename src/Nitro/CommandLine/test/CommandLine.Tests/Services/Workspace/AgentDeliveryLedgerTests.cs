using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="AgentDeliveryLedger"/>'s per-agent delivery
/// reservations directly against a real workspace database.
/// </summary>
public sealed class AgentDeliveryLedgerTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly AgentDatabase _database;
    private readonly AgentDeliveryLedger _ledger;

    public AgentDeliveryLedgerTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-agent-delivery-ledger-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _database = new AgentDatabase();
        _ledger = new AgentDeliveryLedger(_fileSystem, _database);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task ReserveAsync_Should_KeyTheReservationByAgent_When_TwoAgentsReserveTheSameMessage()
    {
        // arrange
        // the same message id reserved for two different agents.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = await InitializeWorkspaceAsync(cancellationToken))
        {
            await SeedAgentAsync(connection, "alice", cancellationToken);
            await SeedAgentAsync(connection, "bob", cancellationToken);
        }
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

        // act
        var aliceReserved = await _ledger.ReserveAsync("alice", ["msg-1"], "ping", now, cancellationToken);
        var bobReserved = await _ledger.ReserveAsync("bob", ["msg-1"], "ping", now, cancellationToken);

        // assert
        // reserving under one agent never blocks the same message id under another.
        Assert.Equal(["msg-1"], aliceReserved);
        Assert.Equal(["msg-1"], bobReserved);
    }

    [Fact]
    public async Task ReserveAsync_Should_ReturnNoClaim_When_TheSameAgentReservesTheSameMessageTwice()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = await InitializeWorkspaceAsync(cancellationToken))
        {
            await SeedAgentAsync(connection, "alice", cancellationToken);
        }
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _ledger.ReserveAsync("alice", ["msg-1"], "ping", now, cancellationToken);

        // act
        var secondReservation = await _ledger.ReserveAsync("alice", ["msg-1"], "ping", now, cancellationToken);

        // assert
        Assert.Empty(secondReservation);
    }

    [Fact]
    public async Task FindDeliveredAsync_Should_ReturnOnlyTheMatchingAgentsMessages_When_AnotherAgentHoldsTheSameId()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = await InitializeWorkspaceAsync(cancellationToken))
        {
            await SeedAgentAsync(connection, "alice", cancellationToken);
            await SeedAgentAsync(connection, "bob", cancellationToken);
        }
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _ledger.ReserveAsync("bob", ["msg-1"], "digest", now, cancellationToken);

        // act
        var aliceDelivered = await _ledger.FindDeliveredAsync("alice", ["msg-1"], cancellationToken);
        var bobDelivered = await _ledger.FindDeliveredAsync("bob", ["msg-1"], cancellationToken);

        // assert
        Assert.Empty(aliceDelivered);
        Assert.Equal(["msg-1"], bobDelivered);
    }

    [Fact]
    public async Task ReleaseAsync_Should_FreeOnlyTheReleasedAgentsReservation_When_TwoAgentsHoldTheSameMessage()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = await InitializeWorkspaceAsync(cancellationToken))
        {
            await SeedAgentAsync(connection, "alice", cancellationToken);
            await SeedAgentAsync(connection, "bob", cancellationToken);
        }
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _ledger.ReserveAsync("alice", ["msg-1"], "ping", now, cancellationToken);
        await _ledger.ReserveAsync("bob", ["msg-1"], "ping", now, cancellationToken);

        // act
        await _ledger.ReleaseAsync("alice", ["msg-1"], "ping", cancellationToken);
        var aliceReReserved = await _ledger.ReserveAsync("alice", ["msg-1"], "ping", now, cancellationToken);
        var bobDelivered = await _ledger.FindDeliveredAsync("bob", ["msg-1"], cancellationToken);

        // assert
        Assert.Equal(["msg-1"], aliceReReserved);
        Assert.Equal(["msg-1"], bobDelivered);
    }

    private async Task<SqliteConnection> InitializeWorkspaceAsync(CancellationToken cancellationToken)
        => await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

    private static async Task SeedAgentAsync(
        SqliteConnection connection, string name, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO agents (name, registered_at, started_at, last_seen_at) VALUES "
            + "(@name, '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');";
        command.Parameters.AddWithValue("@name", name);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
