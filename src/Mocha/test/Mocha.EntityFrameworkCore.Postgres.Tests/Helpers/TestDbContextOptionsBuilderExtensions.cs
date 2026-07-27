using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;

internal static class TestDbContextOptionsBuilderExtensions
{
    // Each test builds its own service provider, so EF accumulates more than twenty
    // internal service providers across a run. Ignore the resulting warning instead of
    // letting it throw.
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
