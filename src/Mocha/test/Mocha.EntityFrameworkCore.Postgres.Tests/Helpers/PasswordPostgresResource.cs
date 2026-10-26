using CookieCrumble.Resources;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;

public sealed class PasswordPostgresResource : PostgreSqlResource
{
    private static readonly TimeSpan s_pollInterval = TimeSpan.FromMilliseconds(50);

    public async Task<string> CreateTestDatabaseAsync()
    {
        var database = $"password_auth_{Guid.NewGuid():N}";
        await CreateDatabaseAsync(database);
        var connectionString = GetConnectionString(database);
        await using var db = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>()
            .UseTestNpgsql(connectionString).Options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        return connectionString;
    }

    public async Task<long> WaitForSessionCountAsync(string applicationName, long expected, TimeSpan timeout)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var deadline = DateTimeOffset.UtcNow + timeout;
        await using var connection = GetConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM pg_stat_activity WHERE application_name = @applicationName";
        command.Parameters.AddWithValue("applicationName", applicationName);

        while (true)
        {
            var sessions = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
            if (sessions == expected || DateTimeOffset.UtcNow >= deadline)
            {
                return sessions;
            }

            await Task.Delay(s_pollInterval, cancellationToken);
        }
    }

    protected override PostgreSqlBuilder Configure(PostgreSqlBuilder builder)
        => builder.WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "scram-sha-256");
}
