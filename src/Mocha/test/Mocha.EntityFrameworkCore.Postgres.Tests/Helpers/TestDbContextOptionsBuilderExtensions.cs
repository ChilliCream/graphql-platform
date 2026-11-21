using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;

internal static class TestDbContextOptionsBuilderExtensions
{
    // A test run builds more than twenty distinct EF Core configurations (data sources, interceptor
    // instances), so EF's process-wide warning about many internal service providers is ignored.
    public static DbContextOptionsBuilder UseTestNpgsql(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString)
        => optionsBuilder.UseNpgsql(connectionString)
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

    public static DbContextOptionsBuilder<TContext> UseTestNpgsql<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        string connectionString)
        where TContext : DbContext
        => optionsBuilder.UseNpgsql(connectionString)
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
}
