using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Connection;

[Collection("Postgres")]
public class PostgresConnectionManagerTests
{
    private readonly PostgresFixture _fixture;

    public PostgresConnectionManagerTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task OpenConnectionAsync_Should_ReturnOpenConnection_When_Called()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var manager = CreateManager(db.ConnectionString);

        // act
        await using var connection = await manager.OpenConnectionAsync(CancellationToken.None);

        // assert
        Assert.NotNull(connection);
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }

    [Fact]
    public async Task OpenConnectionAsync_Should_ReturnMultipleConnections_When_CalledConcurrently()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var manager = CreateManager(db.ConnectionString);

        // act
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => manager.OpenConnectionAsync(CancellationToken.None))
            .ToArray();
        var connections = await Task.WhenAll(tasks);

        // assert
        Assert.Equal(5, connections.Length);

        foreach (var connection in connections)
        {
            Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        }

        // verify they are distinct instances
        var distinct = connections.Distinct().Count();
        Assert.Equal(5, distinct);

        // cleanup
        foreach (var connection in connections)
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task DisposeAsync_Should_Complete_When_Called()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var manager = CreateManager(db.ConnectionString);

        // act
        var exception = await Record.ExceptionAsync(() => manager.DisposeAsync().AsTask());

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_Should_BeIdempotent_When_CalledTwice()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var manager = CreateManager(db.ConnectionString);

        // act
        await manager.DisposeAsync();
        var exception = await Record.ExceptionAsync(() => manager.DisposeAsync().AsTask());

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task EnsureMigratedAsync_Should_CreateSchema_When_DatabaseEmpty()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var manager = CreateManager(db.ConnectionString);

        // act
        await manager.EnsureMigratedAsync(CancellationToken.None);

        // assert - verify the migration tables exist by querying the database
        await using var connection = await manager.OpenConnectionAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name LIKE 'mocha_%';
            """;
        var tableCount = (long)(await command.ExecuteScalarAsync())!;
        Assert.True(tableCount > 0, "Expected mocha tables to exist after migration");
    }

    [Fact]
    public async Task EnsureMigratedAsync_Should_BeIdempotent_When_CalledTwice()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var manager = CreateManager(db.ConnectionString);

        // act
        await manager.EnsureMigratedAsync(CancellationToken.None);
        var exception = await Record.ExceptionAsync(
            () => manager.EnsureMigratedAsync(CancellationToken.None));

        // assert
        Assert.Null(exception);

        // verify schema still intact
        await using var connection = await manager.OpenConnectionAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name LIKE 'mocha_%';
            """;
        var tableCount = (long)(await command.ExecuteScalarAsync())!;
        Assert.True(tableCount > 0, "Expected mocha tables to still exist after second migration");
    }

    [Fact]
    public async Task OpenConnectionAsync_Should_ThrowAfterDispose_When_Disposed()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var manager = CreateManager(db.ConnectionString);
        await manager.DisposeAsync();

        // act & assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => manager.OpenConnectionAsync(CancellationToken.None));
    }

    private static PostgresConnectionManager CreateManager(string connectionString)
    {
        return new PostgresConnectionManager(
            connectionString,
            new PostgresSchemaOptions(),
            NullLoggerFactory.Instance.CreateLogger<PostgresConnectionManager>());
    }
}
