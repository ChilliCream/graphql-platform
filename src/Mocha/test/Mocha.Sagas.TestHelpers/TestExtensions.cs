using Npgsql;

namespace Mocha.Sagas.Tests;

public static class TestExtensions
{
    // TODO: MessageBusMigrator doesn't exist in this codebase - needs to be adapted or removed
    public static async Task MigrateAsync(this NpgsqlConnection connection)
    {
        // Stubbed out - MessageBusMigrator not available
        await Task.CompletedTask;
    }
}
