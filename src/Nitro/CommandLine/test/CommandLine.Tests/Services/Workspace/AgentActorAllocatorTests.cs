using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="AgentActorAllocator"/>: the name pool it draws from, tombstone
/// exclusion, and suffixing once the pool is exhausted.
/// </summary>
public sealed class AgentActorAllocatorTests : IDisposable
{
    private static readonly string[] s_roleWords =
        ["orchestrator", "planner", "implementer", "reviewer", "researcher"];

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentDatabase _database;

    public AgentActorAllocatorTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-agent-actor-allocator-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        _database = new AgentDatabase();
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    // ---------- Pool ----------

    [Fact]
    public void BaseActors_Should_ContainAtLeast300DistinctValidNames()
    {
        // arrange
        var names = AgentActorAllocator.BaseActors;

        // act
        var distinctCount = names.Distinct(StringComparer.Ordinal).Count();

        // assert
        Assert.True(names.Count >= 300, $"Expected at least 300 names, found {names.Count}.");
        Assert.Equal(names.Count, distinctCount);
        Assert.All(names, name => Assert.Equal(name, MailAgentName.Normalize(name)));
    }

    [Fact]
    public void BaseActors_Should_ExcludeRoleWords()
    {
        // act
        var collisions = AgentActorAllocator.BaseActors.Intersect(s_roleWords, StringComparer.Ordinal);

        // assert
        Assert.Empty(collisions);
    }

    // ---------- Tombstone exclusion ----------

    [Fact]
    public async Task AllocateAsync_Should_SkipName_When_ItIsATombstoneInAgents()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var pool = AgentActorAllocator.BaseActors;
        var freeName = pool[0];
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await SeedAgentsAsync(connection, pool.Skip(1), tombstoned: true);

        // act
        var actor = await AllocateAsync(connection, cancellationToken);

        // assert
        Assert.Equal(freeName, actor);
    }

    // ---------- Exhaustion ----------

    [Fact]
    public async Task AllocateAsync_Should_AppendSuffixStartingAtTwo_When_PoolIsExhausted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var pool = AgentActorAllocator.BaseActors;
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await SeedAgentsAsync(connection, pool, tombstoned: false);

        // act
        var actor = await AllocateAsync(connection, cancellationToken);

        // assert
        Assert.EndsWith("-2", actor, StringComparison.Ordinal);
        Assert.Contains(actor[..^2], pool);
    }

    [Fact]
    public async Task AllocateAsync_Should_IncrementSuffix_When_TheFirstSuffixedNameIsAlsoTaken()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var pool = AgentActorAllocator.BaseActors;
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await SeedAgentsAsync(connection, pool, tombstoned: false);
        await SeedAgentsAsync(connection, pool.Select(name => $"{name}-2"), tombstoned: false);

        // act
        var actor = await AllocateAsync(connection, cancellationToken);

        // assert
        Assert.EndsWith("-3", actor, StringComparison.Ordinal);
        Assert.Contains(actor[..^2], pool);
    }

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }

    private static async Task<string> AllocateAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var actor = await AgentActorAllocator.AllocateAsync(connection, transaction);
        await transaction.CommitAsync(cancellationToken);

        return actor;
    }

    /// <summary>
    /// Inserts one bare agents row per name, stamping deleted_at for a tombstone row or
    /// leaving it null for a live one.
    /// </summary>
    private async Task SeedAgentsAsync(SqliteConnection connection, IEnumerable<string> names, bool tombstoned)
    {
        var now = _timeProvider.GetUtcNow();

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "INSERT INTO agents (name, registered_at, started_at, last_seen_at, deleted_at) "
            + "VALUES ($name, $now, $now, $now, $deletedAt);";
        var nameParameter = command.Parameters.Add("$name", SqliteType.Text);
        command.Parameters.AddWithValue("$now", now);
        var deletedAtParameter = command.Parameters.Add("$deletedAt", SqliteType.Text);
        deletedAtParameter.Value = tombstoned ? now : DBNull.Value;

        foreach (var name in names)
        {
            nameParameter.Value = name;
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await transaction.CommitAsync(TestContext.Current.CancellationToken);
    }
}
