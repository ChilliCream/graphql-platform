using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mocha.EntityFrameworkCore;

internal static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Registers the messaging <see cref="IDbContextOptionsExtension"/> on the DbContext options builder,
    /// enabling outbox and saga interceptors to be resolved from the DbContext internal service provider.
    /// </summary>
    /// <param name="optionsBuilder">The DbContext options builder to extend.</param>
    /// <param name="options">The messaging options carrying service configuration delegates.</param>
    /// <returns>The same <paramref name="optionsBuilder"/> instance for chaining.</returns>
    public static DbContextOptionsBuilder AddMessagingExtensions(
        this DbContextOptionsBuilder optionsBuilder,
        MessagingDbContextOptions options)
    {
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(
            new MessagingDbContextOptionsExtension(options));

        return optionsBuilder;
    }
}
