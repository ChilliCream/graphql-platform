using Squadron;

namespace Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlResource _resource = new();

    public async Task InitializeAsync()
    {
        await _resource.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _resource.DisposeAsync();
    }

    public async Task<string> CreateDatabaseAsync()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        await _resource.CreateDatabaseAsync(dbName);
        return _resource.GetConnectionString(dbName);
    }
}
