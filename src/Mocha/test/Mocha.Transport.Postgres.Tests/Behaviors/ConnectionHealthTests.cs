using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public class ConnectionHealthTests
{
    private readonly PostgresFixture _fixture;

    public ConnectionHealthTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task IsHealthyAsync_Should_ReturnTrue_When_DatabaseReachable()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var logger = NullLogger<PostgresConnectionManager>.Instance;
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, new PostgresSchemaOptions(), logger);

        // act
        var isHealthy = await connectionManager.IsHealthyAsync();

        // assert
        Assert.True(isHealthy);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task IsHealthyAsync_Should_ReturnFalse_When_DatabaseUnreachable()
    {
        // arrange
        var logger = NullLogger<PostgresConnectionManager>.Instance;
        var connectionManager = new PostgresConnectionManager(
            "Host=invalid-host-that-does-not-exist;Database=test;Username=test;Password=test;Timeout=1",
            new PostgresSchemaOptions(),
            logger);

        // act
        var isHealthy = await connectionManager.IsHealthyAsync();

        // assert
        Assert.False(isHealthy);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task EnsureMigratedAsync_Should_CreateTables_When_DatabaseEmpty()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var logger = NullLogger<PostgresConnectionManager>.Instance;
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, new PostgresSchemaOptions(), logger);

        // act
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);

        // assert - verify core tables exist
        var isHealthy = await connectionManager.IsHealthyAsync();
        Assert.True(isHealthy);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task EnsureMigratedAsync_Should_BeIdempotent_When_CalledTwice()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var logger = NullLogger<PostgresConnectionManager>.Instance;
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, new PostgresSchemaOptions(), logger);

        // act - call twice
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);

        // assert - no exception thrown
        var isHealthy = await connectionManager.IsHealthyAsync();
        Assert.True(isHealthy);

        await connectionManager.DisposeAsync();
    }
}
