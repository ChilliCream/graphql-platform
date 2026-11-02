using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;

public enum ConnectionConfiguration
{
    ConnectionString,
    DataSource,
    PeriodicPasswordProvider,
    AsyncPasswordProvider
}

public static class ConnectionConfigurationExtensions
{
    public static NpgsqlDataSource CreateDataSource(
        this ConnectionConfiguration configuration,
        string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        if (configuration is ConnectionConfiguration.PeriodicPasswordProvider
            or ConnectionConfiguration.AsyncPasswordProvider)
        {
            var password = builder.ConnectionStringBuilder.Password!;
            builder.ConnectionStringBuilder.Password = null;
            if (configuration == ConnectionConfiguration.PeriodicPasswordProvider)
            {
                builder.UsePeriodicPasswordProvider(
                    (_, _) => ValueTask.FromResult(password), TimeSpan.FromMinutes(30), TimeSpan.FromSeconds(1));
            }
            else
            {
                builder.UsePasswordProvider(_ => password, (_, _) => ValueTask.FromResult(password));
            }
        }

        return builder.Build();
    }
}
