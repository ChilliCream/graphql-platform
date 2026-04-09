using Squadron;

namespace Mocha.EntityFrameworkCore.SqlServer.Tests.Helpers;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly SqlServerResource _resource = new();

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
        return await _resource.CreateDatabaseAsync(dbName);
    }
}
